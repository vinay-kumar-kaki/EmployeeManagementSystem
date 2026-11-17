#!/usr/bin/env node
/*
 * Browser end-to-end test for the Angular frontend against the live API.
 *
 * It drives headless Chrome/Edge through the Chrome DevTools Protocol:
 *   - Admin UI login, dashboard, departments, leave approval, attendance log
 *   - Employee UI login, dashboard, profile, check-out, leave history, leave apply
 *   - Verifies the resulting state through the API afterwards
 *
 * Prerequisites:
 *   - Backend running on http://localhost:5120
 *   - Frontend dev server running on http://localhost:4200
 *   - Chrome or Edge installed (set EMS_BROWSER to override the path)
 *
 * Usage (from repository root):
 *   node tests/e2e/frontend_e2e.js
 */

const fs = require('fs');
const http = require('http');
const os = require('os');
const path = require('path');
const { spawn } = require('child_process');

const wsPath = path.join(__dirname, '..', '..', 'frontend', 'node_modules', 'ws');
const WebSocket = require(wsPath);

const FRONTEND_URL = (process.env.EMS_FRONTEND_URL || 'http://localhost:4200').replace(/\/$/, '');
const API_URL = (process.env.EMS_API_URL || 'http://localhost:5120').replace(/\/$/, '');
const ADMIN_EMAIL = process.env.EMS_ADMIN_EMAIL || 'admin@company.com';
const ADMIN_PASSWORD = process.env.EMS_ADMIN_PASSWORD || 'AdminPassword123!';
const EMPLOYEE_PASSWORD = 'TestPassword123!';
const DEBUG_PORT = Number(process.env.EMS_DEBUG_PORT || 9333);

const RUN_ID = new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 14);
const DEPT_NAME = `UI E2E QA ${RUN_ID}`;
const EMPLOYEE_EMAIL = `ui.e2e.${RUN_ID}@company.com`;
const EMPLOYEE_FIRST = 'UI';
const EMPLOYEE_LAST = `Tester${RUN_ID.slice(-4)}`;
const EMPLOYEE_FULL = `${EMPLOYEE_FIRST} ${EMPLOYEE_LAST}`;
const APPROVAL_REMARKS = `Approved via UI test ${RUN_ID}`;
const NEW_LEAVE_REASON = `UI applied leave ${RUN_ID}`;

function utcDatePlus(days) {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

const LEAVE_START = utcDatePlus(3);
const LEAVE_END = utcDatePlus(4);
const SECOND_LEAVE_START = utcDatePlus(7);
const SECOND_LEAVE_END = utcDatePlus(8);

const results = [];
let browser = null;
let cdp = null;
let userDataDir = null;

function pass(step, details = '') {
  results.push(step);
  console.log(`  PASS  ${step}` + (details ? ` - ${details}` : ''));
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

function httpRequest(method, url) {
  return new Promise((resolve, reject) => {
    const req = http.request(url, { method }, res => {
      let body = '';
      res.on('data', chunk => { body += chunk; });
      res.on('end', () => resolve({ status: res.statusCode, body }));
    });
    req.on('error', reject);
    req.setTimeout(3000, () => req.destroy(new Error('HTTP timeout')));
    req.end();
  });
}

async function httpJson(url) {
  const response = await httpRequest('GET', url);
  return JSON.parse(response.body);
}

async function api(method, apiPath, token, body) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(API_URL + apiPath, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  const text = await response.text();
  let payload = null;
  try {
    payload = text ? JSON.parse(text) : null;
  } catch {
    payload = text;
  }

  if (!response.ok) {
    throw new Error(`${method} ${apiPath} returned ${response.status}: ${text.slice(0, 500)}`);
  }
  return payload;
}

class CdpClient {
  constructor(wsUrl) {
    this.ws = new WebSocket(wsUrl, { perMessageDeflate: false });
    this.nextId = 1;
    this.pending = new Map();
    this.ready = new Promise((resolve, reject) => {
      this.ws.on('open', resolve);
      this.ws.on('error', reject);
    });
    this.ws.on('message', data => {
      const message = JSON.parse(data.toString());
      if (message.id) {
        const pending = this.pending.get(message.id);
        if (!pending) return;
        this.pending.delete(message.id);
        if (message.error) {
          pending.reject(new Error(JSON.stringify(message.error)));
        } else {
          pending.resolve(message.result);
        }
      }
    });
  }

  async send(method, params = {}) {
    await this.ready;
    const id = this.nextId++;
    return new Promise((resolve, reject) => {
      this.pending.set(id, { resolve, reject });
      this.ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async evaluate(expression) {
    const result = await this.send('Runtime.evaluate', {
      expression,
      returnByValue: true,
      awaitPromise: true,
      userGesture: true
    });

    if (result.exceptionDetails) {
      const description = result.exceptionDetails.exception?.description
        || result.exceptionDetails.text
        || 'Unknown evaluation error';
      throw new Error(`Browser evaluation failed: ${description}`);
    }
    return result.result.value;
  }

  close() {
    try { this.ws.close(); } catch { /* ignore */ }
  }
}

function findBrowser() {
  const candidates = [
    process.env.EMS_BROWSER,
    'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
    'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
    'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe'
  ].filter(Boolean);

  const found = candidates.find(candidate => fs.existsSync(candidate));
  if (!found) {
    throw new Error('No Chrome/Edge executable found. Set EMS_BROWSER to a browser path.');
  }
  return found;
}

async function waitForDebugger(timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    try {
      const version = await httpJson(`http://127.0.0.1:${DEBUG_PORT}/json/version`);
      if (version.webSocketDebuggerUrl) return;
    } catch {
      // browser not ready yet
    }
    await sleep(250);
  }
  throw new Error('Browser DevTools endpoint did not become ready.');
}

async function connectToPage() {
  const list = await httpJson(`http://127.0.0.1:${DEBUG_PORT}/json/list`);
  let page = list.find(target => target.type === 'page');
  if (!page) {
    const created = await httpRequest('PUT', `http://127.0.0.1:${DEBUG_PORT}/json/new?about:blank`);
    page = JSON.parse(created.body);
  }
  if (!page || !page.webSocketDebuggerUrl) {
    throw new Error('Unable to find a browser page target.');
  }

  const client = new CdpClient(page.webSocketDebuggerUrl);
  await client.ready;
  await client.send('Page.enable');
  await client.send('Runtime.enable');
  return client;
}

async function waitFor(expression, description, timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  let lastError = '';

  while (Date.now() < deadline) {
    try {
      const value = await cdp.evaluate(`(() => { try { return !!(${expression}); } catch (e) { return false; } })()`);
      if (value) return;
    } catch (error) {
      lastError = error.message;
    }
    await sleep(250);
  }

  let body = '';
  try {
    body = await cdp.evaluate('document.body ? document.body.innerText.slice(0, 1500) : ""');
  } catch {
    // ignore
  }

  throw new Error(
    `Timed out waiting for ${description}.` +
    (lastError ? ` Last error: ${lastError}` : '') +
    (body ? `\nVisible text:\n${body}` : '')
  );
}

async function expectText(text, description) {
  // Case-insensitive: CSS text-transform changes innerText casing for labels.
  const haystack = JSON.stringify(text.toLowerCase());
  await waitFor(
    `document.body.innerText.toLowerCase().includes(${haystack})`,
    description || `text "${text}"`
  );
  pass(description || `Saw "${text}"`);
}

async function navigate(routePath) {
  const url = FRONTEND_URL + routePath;
  await cdp.send('Page.navigate', { url });
  await waitFor(
    `location.pathname === ${JSON.stringify(routePath)} && document.readyState === 'complete'`,
    `navigation to ${routePath}`
  );
  await waitFor(
    `document.querySelector('app-root') && document.querySelector('app-root').innerText.trim().length > 0`,
    `Angular render on ${routePath}`
  );
}

async function setInput(selector, value) {
  const result = await cdp.evaluate(`(() => {
    const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return 'not-found';
    const proto = el instanceof HTMLTextAreaElement
      ? HTMLTextAreaElement.prototype
      : HTMLInputElement.prototype;
    const setter = Object.getOwnPropertyDescriptor(proto, 'value').set;
    setter.call(el, ${JSON.stringify(value)});
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    el.dispatchEvent(new Event('blur', { bubbles: true }));
    return el.value;
  })()`);

  if (result === 'not-found') {
    throw new Error(`Input not found: ${selector}`);
  }
}

async function selectFirstOption(selector) {
  const result = await cdp.evaluate(`(() => {
    const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return 'not-found';
    if (el.options.length < 2) return 'no-options';
    el.selectedIndex = 1;
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return el.options[el.selectedIndex].textContent.trim();
  })()`);

  if (result === 'not-found' || result === 'no-options') {
    throw new Error(`Select not ready: ${selector} (${result})`);
  }
  return result;
}

async function clickSelector(selector) {
  const result = await cdp.evaluate(`(() => {
    const el = document.querySelector(${JSON.stringify(selector)});
    if (!el) return 'not-found';
    el.click();
    return 'clicked';
  })()`);

  if (result !== 'clicked') {
    throw new Error(`Element not found for click: ${selector}`);
  }
}

async function clickByText(selector, text) {
  const result = await cdp.evaluate(`(() => {
    const elements = Array.from(document.querySelectorAll(${JSON.stringify(selector)}));
    const target = elements.find(el => (el.textContent || '').replace(/\\s+/g, ' ').trim().includes(${JSON.stringify(text)}));
    if (!target) return 'not-found';
    target.click();
    return 'clicked';
  })()`);

  if (result !== 'clicked') {
    throw new Error(`Element not found: ${selector} containing "${text}"`);
  }
}

async function clickInRow(rowText, buttonText) {
  const result = await cdp.evaluate(`(() => {
    const rows = Array.from(document.querySelectorAll('tbody tr'));
    const row = rows.find(r => (r.innerText || '').includes(${JSON.stringify(rowText)}));
    if (!row) return 'row-not-found';
    const button = Array.from(row.querySelectorAll('button'))
      .find(b => (b.innerText || '').includes(${JSON.stringify(buttonText)}));
    if (!button) return 'button-not-found';
    button.click();
    return 'clicked';
  })()`);

  if (result !== 'clicked') {
    throw new Error(`Could not click "${buttonText}" in row containing "${rowText}" (${result})`);
  }
}

async function login(email, password, expectedName) {
  await navigate('/login');
  await waitFor(
    `document.querySelector('input[formcontrolname="email"]') && document.querySelector('button[type="submit"]')`,
    'login form'
  );

  await setInput('input[formcontrolname="email"]', email);
  await setInput('input[formcontrolname="password"]', password);
  await clickSelector('button[type="submit"]');

  await waitFor(`location.pathname === '/dashboard'`, 'redirect to dashboard after login');
  await expectText(expectedName, `Logged in and dashboard shows ${expectedName}`);

  const hasToken = await cdp.evaluate(`!!localStorage.getItem('ems_auth_token')`);
  if (!hasToken) throw new Error('JWT was not stored after login');
  pass('JWT persisted in browser localStorage');
}

async function logout() {
  await clickByText('button', 'Logout');
  await waitFor(`location.pathname === '/login'`, 'redirect to login after logout');
  pass('Logout returns to login page');
}

async function getToken(email, password) {
  const response = await api('POST', '/api/auth/login', null, { email, password });
  return response.token;
}

async function main() {
  console.log('='.repeat(72));
  console.log('Employee Management System - Browser E2E Test');
  console.log(`Frontend: ${FRONTEND_URL}`);
  console.log(`API     : ${API_URL}`);
  console.log(`Run     : ${RUN_ID}`);
  console.log('='.repeat(72));

  // ------------------------------------------------------------------
  // Seed API data used by the UI flows
  // ------------------------------------------------------------------
  console.log('\n[0] Seeding data through the API');
  const adminResponse = await api('POST', '/api/auth/login', null, {
    email: ADMIN_EMAIL,
    password: ADMIN_PASSWORD
  });
  const adminToken = adminResponse.token;
  pass('Admin API login', adminResponse.role);

  const department = await api('POST', '/api/department', adminToken, {
    name: DEPT_NAME,
    description: 'Created by frontend E2E test'
  });
  pass('API department created', `${department.name} (ID ${department.id})`);

  const employee = await api('POST', '/api/employee', adminToken, {
    firstName: EMPLOYEE_FIRST,
    lastName: EMPLOYEE_LAST,
    email: EMPLOYEE_EMAIL,
    password: EMPLOYEE_PASSWORD,
    phoneNumber: '+1 555 0111',
    departmentId: department.id,
    designation: 'UI Automation Engineer',
    joiningDate: utcDatePlus(0),
    salary: 68000
  });
  pass('API employee created', `${employee.fullName} (ID ${employee.id})`);

  const employeeResponse = await api('POST', '/api/auth/login', null, {
    email: EMPLOYEE_EMAIL,
    password: EMPLOYEE_PASSWORD
  });
  const employeeToken = employeeResponse.token;

  await api('POST', '/api/attendance/check-in', employeeToken);
  pass('API check-in created attendance record');

  const leaveTypes = await api('GET', '/api/leave/types', employeeToken);
  const leaveTypeId = leaveTypes[0].id;
  const seededLeave = await api('POST', '/api/leave/apply', employeeToken, {
    leaveTypeId,
    startDate: LEAVE_START,
    endDate: LEAVE_END,
    reason: `Seeded leave for UI review ${RUN_ID}`
  });
  pass('API leave created for admin review', `ID ${seededLeave.id}, ${seededLeave.totalDays} days`);

  // ------------------------------------------------------------------
  // Start headless browser
  // ------------------------------------------------------------------
  console.log('\n[1] Starting headless browser');
  const browserPath = findBrowser();
  userDataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'ems-e2e-'));
  browser = spawn(browserPath, [
    '--headless=new',
    '--disable-gpu',
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-extensions',
    '--disable-background-networking',
    '--window-size=1400,1000',
    '--remote-allow-origins=*',
    `--remote-debugging-port=${DEBUG_PORT}`,
    `--user-data-dir=${userDataDir}`,
    'about:blank'
  ], { stdio: 'ignore' });

  await waitForDebugger();
  cdp = await connectToPage();
  pass('Headless browser connected', path.basename(browserPath));

  // ------------------------------------------------------------------
  // Admin UI flow
  // ------------------------------------------------------------------
  console.log('\n[2] Admin UI flow');
  await login(ADMIN_EMAIL, ADMIN_PASSWORD, 'System Admin');
  await expectText('Active Staff', 'Admin dashboard shows active staff metric');
  await expectText('Pending Leaves', 'Admin dashboard shows pending leaves metric');
  await expectText('Role: Admin', 'Admin role badge visible');

  await navigate('/departments');
  await expectText(DEPT_NAME, 'Admin department list shows seeded department');
  await expectText('UI E2E', 'Department table rendered');

  await navigate('/employees');
  await expectText(EMPLOYEE_FULL, 'Admin employee directory shows seeded employee');
  await expectText(EMPLOYEE_EMAIL, 'Employee email visible in directory');

  await navigate('/leaves/review');
  await waitFor(
    `document.body.innerText.includes(${JSON.stringify(EMPLOYEE_FULL)})`,
    'pending leave row for seeded employee'
  );
  pass('Admin pending leave queue shows employee request');

  await clickInRow(EMPLOYEE_FULL, 'Approve');
  await waitFor(
    `document.body.innerText.includes('Approve Leave Request')`,
    'approve leave modal'
  );
  pass('Approve modal opened');

  await setInput('.modal textarea', APPROVAL_REMARKS);
  await clickByText('.modal button', 'Confirm Approval');
  await expectText('Leave request marked as Approved.', 'Approval success alert');
  pass('Admin approved leave through UI');

  await clickByText('button', 'All Requests');
  await waitFor(
    `document.body.innerText.includes(${JSON.stringify(APPROVAL_REMARKS)})`,
    'approved leave with remarks in All Requests tab'
  );
  pass('All Requests tab shows approved leave and remarks');

  await navigate('/attendance/log');
  await expectText('Company Attendance Log', 'Admin attendance log page loaded');
  await expectText(EMPLOYEE_FULL, 'Attendance log shows checked-in employee');
  await expectText('In Shift', 'Attendance log shows in-shift status');
  pass('Admin attendance log displays live database record');

  await logout();

  // ------------------------------------------------------------------
  // Employee UI flow
  // ------------------------------------------------------------------
  console.log('\n[3] Employee UI flow');
  await login(EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD, EMPLOYEE_FULL);
  await expectText('Apply For Leave', 'Employee dashboard shows leave action');
  await expectText('Role: Employee', 'Employee role badge visible');

  await navigate('/profile');
  await expectText(EMPLOYEE_FULL, 'Profile shows employee name');
  await expectText(DEPT_NAME, 'Profile shows department');
  await expectText('UI Automation Engineer', 'Profile shows designation');
  await expectText('Active Employee', 'Profile shows active status');

  await navigate('/attendance');
  await waitFor(
    `document.body.innerText.toLowerCase().includes('clock out now')`,
    'clock-out button for checked-in employee'
  );
  pass('Attendance tracker recognises existing check-in');
  await clickByText('button', 'Clock Out Now');
  await waitFor(
    `document.body.innerText.toLowerCase().includes('shift completed for today')`,
    'shift completed state after check-out'
  );
  await expectText('Total Logged Time', 'Attendance tracker shows logged duration');
  pass('Employee checked out through UI');

  await navigate('/leaves/history');
  await expectText('Approved', 'Leave history shows approved status');
  await expectText(APPROVAL_REMARKS, 'Leave history shows admin remarks saved via UI');

  await navigate('/leaves/apply');
  await waitFor(
    `document.querySelectorAll('select[formcontrolname="leaveTypeId"] option').length > 1`,
    'leave type options loaded'
  );
  const selectedLeaveType = await selectFirstOption('select[formcontrolname="leaveTypeId"]');
  pass('Leave type selected', selectedLeaveType);

  await setInput('input[formcontrolname="startDate"]', SECOND_LEAVE_START);
  await setInput('input[formcontrolname="endDate"]', SECOND_LEAVE_END);
  await setInput('textarea[formcontrolname="reason"]', NEW_LEAVE_REASON);
  await clickByText('button', 'Submit Leave Application');

  await waitFor(`location.pathname === '/leaves/history'`, 'redirect to leave history after apply');
  await expectText(NEW_LEAVE_REASON, 'Newly applied leave appears in history');
  await expectText('Pending', 'Newly applied leave is pending');
  pass('Employee applied for leave through UI');

  await logout();

  // ------------------------------------------------------------------
  // Verify UI writes through the API
  // ------------------------------------------------------------------
  console.log('\n[4] Verifying UI actions through the API');
  const freshEmployeeToken = await getToken(EMPLOYEE_EMAIL, EMPLOYEE_PASSWORD);

  const history = await api('GET', '/api/leave/my-history', freshEmployeeToken);
  const approved = history.find(l => l.id === seededLeave.id);
  const pending = history.find(l => l.reason === NEW_LEAVE_REASON);

  if (!approved || approved.status !== 1 || approved.adminRemarks !== APPROVAL_REMARKS) {
    throw new Error('UI approval did not persist correctly through the API.');
  }
  pass('API confirms UI-approved leave', `ID ${approved.id}, Approved`);

  if (!pending || pending.status !== 0) {
    throw new Error('UI-applied leave did not persist correctly through the API.');
  }
  pass('API confirms UI-applied leave', `ID ${pending.id}, Pending`);

  const todayStatus = await api('GET', '/api/attendance/today-status', freshEmployeeToken);
  if (!todayStatus.hasCheckedOutToday) {
    throw new Error('UI check-out did not persist through the API.');
  }
  pass('API confirms UI check-out', `duration=${todayStatus.workDurationMinutes}m`);

  const adminStats = await api('GET', '/api/dashboard/admin-stats', adminToken);
  if (adminStats.totalEmployees < 2 || adminStats.presentTodayCount < 1) {
    throw new Error('Admin dashboard metrics did not reflect the seeded data.');
  }
  pass('API dashboard metrics reflect UI data',
    `staff=${adminStats.totalEmployees}, present=${adminStats.presentTodayCount}`);

  console.log('\n' + '='.repeat(72));
  console.log(`ALL ${results.length} BROWSER E2E CHECKS PASSED`);
  console.log('='.repeat(72));
  return 0;
}

async function cleanup() {
  if (cdp) {
    try { await cdp.send('Browser.close'); } catch { /* ignore */ }
    cdp.close();
  }
  if (browser && !browser.killed) {
    try { browser.kill(); } catch { /* ignore */ }
  }
  if (userDataDir) {
    try { fs.rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }
}

main()
  .then(async code => {
    await cleanup();
    process.exit(code);
  })
  .catch(async error => {
    console.error('\n' + '='.repeat(72));
    console.error('BROWSER E2E TEST FAILED');
    console.error('='.repeat(72));
    console.error(error.message);
    console.error(`Passed before failure: ${results.length}`);
    await cleanup();
    process.exit(1);
  });
