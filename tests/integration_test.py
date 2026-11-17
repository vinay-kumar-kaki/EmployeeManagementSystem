#!/usr/bin/env python3
"""
End-to-end integration test for the Employee Management System.

Exercises the live ASP.NET Core API (frontend contract) and verifies that the
resulting state is persisted in the SQLite database.

Prerequisites:
    * Backend running on http://localhost:5120 (see README.md)
    * Python 3.8+ (standard library only)

Usage:
    python tests/integration_test.py
    set EMS_API_URL=http://localhost:5120 && python tests/integration_test.py
"""

import json
import os
import sqlite3
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timedelta
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE_URL = os.environ.get("EMS_API_URL", "http://localhost:5120").rstrip("/")
DB_PATH = Path(os.environ.get(
    "EMS_DB_PATH",
    ROOT / "backend" / "EmployeeManagementSystem.API" / "EmployeeManagement.db",
))
ADMIN_EMAIL = os.environ.get("EMS_ADMIN_EMAIL", "admin@company.com")
ADMIN_PASSWORD = os.environ.get("EMS_ADMIN_PASSWORD", "admin@123")
EMPLOYEE_PASSWORD = "employee@123"

RUN_ID = datetime.now().strftime("%Y%m%d%H%M%S")
DEPT_NAME = f"Integration QA {RUN_ID}"
EMPLOYEE_EMAIL = f"integration.{RUN_ID}@company.com"
EMPLOYEE_FIRST = "Int"
EMPLOYEE_LAST = f"Tester{RUN_ID[-4:]}"

# The API computes "today" from the server's local calendar date.
TODAY = datetime.now().date()
LEAVE_START = TODAY + timedelta(days=3)
LEAVE_END = TODAY + timedelta(days=4)

results = []


def record(step, details=""):
    results.append((step, details))
    print(f"  PASS  {step}" + (f" - {details}" if details else ""))


def api(method, path, token=None, body=None, expected=200):
    url = BASE_URL + path
    data = json.dumps(body).encode("utf-8") if body is not None else None
    request = urllib.request.Request(url, data=data, method=method)
    request.add_header("Content-Type", "application/json")
    request.add_header("Accept", "application/json")
    if token:
        request.add_header("Authorization", f"Bearer {token}")

    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            raw = response.read()
            payload = json.loads(raw.decode("utf-8")) if raw else None
            status = response.status
    except urllib.error.HTTPError as exc:
        raw = exc.read()
        payload = json.loads(raw.decode("utf-8")) if raw else None
        status = exc.code

    expected_codes = expected if isinstance(expected, (list, tuple, set)) else [expected]
    if status not in expected_codes:
        raise AssertionError(
            f"{method} {path} returned HTTP {status}, expected {expected_codes}. "
            f"Payload: {json.dumps(payload, default=str)[:800]}"
        )
    return status, payload


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def main():
    print("=" * 72)
    print("Employee Management System - Integration Test")
    print(f"API: {BASE_URL}")
    print(f"DB : {DB_PATH}")
    print(f"Run: {RUN_ID}")
    print("=" * 72)

    if not DB_PATH.exists():
        raise AssertionError(f"Database not found: {DB_PATH}. Start the backend once to seed it.")

    # ------------------------------------------------------------------
    print("\n[1] Authentication")
    # ------------------------------------------------------------------
    status, body = api("POST", "/api/auth/login", body={
        "email": ADMIN_EMAIL,
        "password": ADMIN_PASSWORD,
    })
    require(body and body.get("success") is True, "Admin login did not return success=true")
    require(body.get("role") == "Admin", f"Expected role Admin, got {body.get('role')}")
    admin_token = body["token"]
    record("Admin login", f"role={body['role']}")

    status, body = api("POST", "/api/auth/login", body={
        "email": ADMIN_EMAIL,
        "password": "DefinitelyWrong!123",
    }, expected=401)
    require(body and body.get("success") is False, "Invalid login should return success=false")
    record("Invalid password rejected", "HTTP 401")

    # ------------------------------------------------------------------
    print("\n[2] Department management")
    # ------------------------------------------------------------------
    status, dept = api("POST", "/api/department", token=admin_token, body={
        "name": DEPT_NAME,
        "description": "Created by automated integration test",
    }, expected=201)
    require(dept and dept.get("id"), "Department creation did not return an id")
    dept_id = dept["id"]
    record("Department created", f"{dept['name']} (ID {dept_id})")

    status, body = api("POST", "/api/department", token=admin_token, body={
        "name": DEPT_NAME,
        "description": "duplicate",
    }, expected=400)
    require("message" in (body or {}), "Duplicate department should return an error message")
    record("Duplicate department rejected", body["message"])

    status, depts = api("GET", "/api/department?activeOnly=true", token=admin_token)
    require(any(d["id"] == dept_id for d in depts), "Created department missing from list")
    record("Department visible in list", f"{len(depts)} active departments")

    # ------------------------------------------------------------------
    print("\n[3] Employee management")
    # ------------------------------------------------------------------
    status, employee = api("POST", "/api/employee", token=admin_token, body={
        "firstName": EMPLOYEE_FIRST,
        "lastName": EMPLOYEE_LAST,
        "email": EMPLOYEE_EMAIL,
        "password": EMPLOYEE_PASSWORD,
        "phoneNumber": "+1 555 0199",
        "departmentId": dept_id,
        "designation": "Integration Test Engineer",
        "joiningDate": TODAY.isoformat(),
        "salary": 72000,
    }, expected=201)
    require(employee and employee.get("id"), "Employee creation did not return an id")
    employee_id = employee["id"]
    record("Employee created", f"{employee['fullName']} (ID {employee_id})")

    status, body = api("POST", "/api/employee", token=admin_token, body={
        "firstName": "Duplicate",
        "lastName": "User",
        "email": EMPLOYEE_EMAIL,
        "password": EMPLOYEE_PASSWORD,
        "departmentId": dept_id,
        "designation": "N/A",
        "joiningDate": TODAY.isoformat(),
        "salary": 1000,
    }, expected=400)
    require("message" in (body or {}), "Duplicate employee email should return an error message")
    record("Duplicate employee email rejected", body["message"])

    status, employees = api(
        "GET",
        "/api/employee?search=" + urllib.parse.quote(EMPLOYEE_EMAIL),
        token=admin_token,
    )
    require(len(employees) == 1 and employees[0]["id"] == employee_id,
            "Employee search did not find the created employee")
    record("Employee search works", "1 matching result")

    # ------------------------------------------------------------------
    print("\n[4] Employee login and profile")
    # ------------------------------------------------------------------
    status, body = api("POST", "/api/auth/login", body={
        "email": EMPLOYEE_EMAIL,
        "password": EMPLOYEE_PASSWORD,
    })
    require(body and body.get("role") == "Employee", "Employee login did not return Employee role")
    require(body.get("employeeId") == employee_id, "Employee login returned wrong employeeId")
    employee_token = body["token"]
    record("Employee login", f"role={body['role']}, employeeId={body['employeeId']}")

    status, me = api("GET", "/api/auth/me", token=employee_token)
    require(me.get("email") == EMPLOYEE_EMAIL, "Current user email mismatch")
    require(me.get("employeeId") == employee_id, "Current user employeeId mismatch")
    record("GET /auth/me", me["email"])

    status, profile = api("GET", "/api/employee/profile/me", token=employee_token)
    require(profile.get("departmentName") == DEPT_NAME, "Profile department mismatch")
    require(profile.get("designation") == "Integration Test Engineer", "Profile designation mismatch")
    require(profile.get("phoneNumber") == "+1 555 0199", "Profile phone mismatch")
    record("Employee profile", f"{profile['fullName']} | {profile['departmentName']}")

    # ------------------------------------------------------------------
    print("\n[5] Attendance")
    # ------------------------------------------------------------------
    status, status_today = api("GET", "/api/attendance/today-status", token=employee_token)
    require(status_today.get("hasCheckedInToday") is False, "New employee should not be checked in")
    record("Initial attendance status", "Not checked in")

    status, checkin = api("POST", "/api/attendance/check-in", token=employee_token)
    require(checkin.get("hasCheckedInToday") is True, "Check-in did not mark attendance")
    require(checkin.get("status") == "Present", f"Expected Present, got {checkin.get('status')}")
    record("Check-in", f"{checkin['status']} at {checkin['checkInTime']}")

    status, body = api("POST", "/api/attendance/check-in", token=employee_token, expected=400)
    require("message" in (body or {}), "Second check-in should be rejected")
    record("Duplicate check-in rejected", body["message"])

    # ------------------------------------------------------------------
    print("\n[6] Leave management")
    # ------------------------------------------------------------------
    status, leave_types = api("GET", "/api/leave/types", token=employee_token)
    require(len(leave_types) >= 1, "No leave types returned")
    leave_type = next((lt for lt in leave_types if lt["isActive"]), leave_types[0])
    record("Leave types loaded", f"{len(leave_types)} types (using {leave_type['name']})")

    # Admin leave-type CRUD (new in this version)
    lt_name = f"Integration QA Leave {RUN_ID}"
    status, created_type = api("POST", "/api/leave/types", token=admin_token, body={
        "name": lt_name,
        "defaultDaysPerYear": 7,
        "isActive": True,
    })
    require(created_type.get("id", 0) > 0, "Could not create leave type")
    leave_type_id = created_type["id"]
    record("Leave type created", f"ID {leave_type_id}")

    status, duplicate_type = api("POST", "/api/leave/types", token=admin_token,
                                 body={"name": lt_name, "defaultDaysPerYear": 7, "isActive": True},
                                 expected=400)
    require("message" in (duplicate_type or {}), "Duplicate leave type should be rejected")
    record("Duplicate leave type rejected", duplicate_type["message"])

    status, updated_type = api("PUT", f"/api/leave/types/{leave_type_id}", token=admin_token, body={
        "name": f"{lt_name} Updated",
        "defaultDaysPerYear": 9,
        "isActive": False,
    })
    require(updated_type.get("defaultDaysPerYear") == 9 and updated_type.get("isActive") is False,
            "Leave type update did not persist")
    record("Leave type updated", f"ID {leave_type_id}, 9 days, inactive")

    status, all_types = api("GET", "/api/leave/types/all", token=admin_token)
    require(any(t["id"] == leave_type_id and t["isActive"] is False for t in all_types),
            "Updated leave type missing from admin all-types view")
    record("Admin all-leave-types view", f"{len(all_types)} type(s)")

    status, leave = api("POST", "/api/leave/apply", token=employee_token, body={
        "leaveTypeId": leave_type["id"],
        "startDate": LEAVE_START.isoformat(),
        "endDate": LEAVE_END.isoformat(),
        "reason": "Integration test leave request covering two days.",
    })
    require(leave.get("status") == 0, f"New leave should be Pending (0), got {leave.get('status')}")
    require(leave.get("totalDays") == 2, f"Expected 2 leave days, got {leave.get('totalDays')}")
    leave_id = leave["id"]
    record("Leave applied", f"ID {leave_id}, {leave['totalDays']} days, Pending")

    status, history = api("GET", "/api/leave/my-history", token=employee_token)
    require(any(l["id"] == leave_id for l in history), "Applied leave missing from employee history")
    record("Employee leave history", f"{len(history)} request(s)")

    status, pending = api("GET", "/api/leave/pending", token=admin_token)
    require(any(l["id"] == leave_id for l in pending), "Leave missing from admin pending queue")
    record("Admin pending queue", f"{len(pending)} pending request(s)")

    status, reviewed = api("PUT", f"/api/leave/{leave_id}/review", token=admin_token, body={
        "status": 1,
        "adminRemarks": "Approved by integration test.",
    })
    require(reviewed.get("status") == 1, "Leave review did not approve the request")
    require(reviewed.get("adminRemarks") == "Approved by integration test.", "Admin remarks not saved")
    record("Admin approved leave", f"ID {leave_id}, remarks saved")

    status, all_leaves = api("GET", "/api/leave/all", token=admin_token)
    require(any(l["id"] == leave_id and l["status"] == 1 for l in all_leaves),
            "Approved leave missing from /leave/all")
    record("Admin all-leaves view", f"{len(all_leaves)} request(s)")

    # ------------------------------------------------------------------
    print("\n[7] Admin attendance log + dashboards")
    # ------------------------------------------------------------------
    status, attendance = api(
        "GET", f"/api/attendance/all?date={TODAY.isoformat()}", token=admin_token
    )
    matching = [a for a in attendance if a["employeeId"] == employee_id]
    require(len(matching) == 1, "Employee attendance missing from admin log")
    require(matching[0]["employeeName"] == f"{EMPLOYEE_FIRST} {EMPLOYEE_LAST}",
            "Attendance log employee name mismatch")
    record("Admin attendance log", f"{len(attendance)} record(s) today")

    status, admin_stats = api("GET", "/api/dashboard/admin-stats", token=admin_token)
    require(admin_stats["totalEmployees"] >= 2, "Admin stats totalEmployees should include test employee")
    require(admin_stats["activeDepartments"] >= 4, "Admin stats activeDepartments unexpectedly low")
    require(admin_stats["presentTodayCount"] >= 1, "Admin stats presentTodayCount unexpectedly low")
    record("Admin dashboard",
           f"staff={admin_stats['totalEmployees']}, depts={admin_stats['activeDepartments']}, "
           f"present={admin_stats['presentTodayCount']}")

    status, employee_stats = api("GET", "/api/dashboard/employee-stats", token=employee_token)
    require(employee_stats["approvedLeaves"] >= 1, "Employee stats should show approved leave")
    require(employee_stats["hasCheckedInToday"] is True, "Employee stats should show check-in")
    record("Employee dashboard",
           f"approved={employee_stats['approvedLeaves']}, "
           f"today={employee_stats['formattedTodayHours']}")

    # ------------------------------------------------------------------
    print("\n[8] Check-out")
    # ------------------------------------------------------------------
    status, checkout = api("POST", "/api/attendance/check-out", token=employee_token)
    require(checkout.get("hasCheckedOutToday") is True, "Check-out did not complete")
    require(checkout.get("workDurationMinutes", 0) >= 1, "Work duration was not recorded")
    record("Check-out", f"duration={checkout['workDurationMinutes']}m")

    status, body = api("POST", "/api/attendance/check-out", token=employee_token, expected=400)
    require("message" in (body or {}), "Second check-out should be rejected")
    record("Duplicate check-out rejected", body["message"])

    # ------------------------------------------------------------------
    print("\n[9] Authorization boundaries")
    # ------------------------------------------------------------------
    api("GET", "/api/employee", expected=401)
    record("Unauthenticated admin endpoint rejected", "HTTP 401")

    api("GET", "/api/employee", token=employee_token, expected=403)
    record("Employee blocked from admin endpoint", "HTTP 403")

    api("GET", "/api/leave/pending", token=employee_token, expected=403)
    record("Employee blocked from leave review queue", "HTTP 403")

    api("PUT", f"/api/leave/{leave_id}/review", token=employee_token,
        body={"status": 2, "adminRemarks": "should fail"}, expected=403)
    record("Employee blocked from reviewing leave", "HTTP 403")

    api("POST", "/api/auth/register-admin", body={
        "firstName": "Should",
        "lastName": "Fail",
        "email": f"unauthorized.{RUN_ID}@company.com",
        "password": EMPLOYEE_PASSWORD,
    }, expected=401)
    record("Unauthenticated admin registration rejected", "HTTP 401")

    # ------------------------------------------------------------------
    print("\n[10] Database persistence verification")
    # ------------------------------------------------------------------
    uri = f"file:{DB_PATH}?mode=ro"
    con = sqlite3.connect(uri, uri=True)
    try:
        cur = con.cursor()

        row = cur.execute(
            "SELECT Id, DepartmentId, IsActive, Designation FROM Employees WHERE Email = ?",
            (EMPLOYEE_EMAIL,),
        ).fetchone()
        require(row is not None, "Employee row not found in database")
        require(row[0] == employee_id, "Database employee id mismatch")
        require(row[1] == dept_id, "Database department id mismatch")
        require(row[2] == 1, "Database employee should be active")
        require(row[3] == "Integration Test Engineer", "Database designation mismatch")
        record("DB employee row persisted", f"ID {row[0]}, dept {row[1]}, active={row[2]}")

        row = cur.execute(
            "SELECT u.Email, r.Name FROM AspNetUserRoles ur "
            "JOIN AspNetUsers u ON u.Id = ur.UserId "
            "JOIN AspNetRoles r ON r.Id = ur.RoleId "
            "WHERE u.Email = ?",
            (EMPLOYEE_EMAIL,),
        ).fetchone()
        require(row is not None and row[1] == "Employee", "Employee role mapping missing in database")
        record("DB identity role persisted", f"{row[0]} -> {row[1]}")

        row = cur.execute(
            "SELECT Status, AdminRemarks, TotalDays FROM LeaveRequests WHERE Id = ?",
            (leave_id,),
        ).fetchone()
        require(row is not None, "Leave request row not found in database")
        require(row[0] == 1, f"Leave status in DB should be 1 (Approved), got {row[0]}")
        require(row[1] == "Approved by integration test.", "Leave remarks not persisted in DB")
        require(row[2] == 2, f"Leave total days in DB should be 2, got {row[2]}")
        record("DB leave request persisted", f"ID {leave_id}, status={row[0]}, days={row[2]}")

        row = cur.execute(
            "SELECT CheckInTime, CheckOutTime, WorkDurationMinutes, Status "
            "FROM Attendances WHERE EmployeeId = ? AND Date LIKE ?",
            (employee_id, f"{TODAY.isoformat()}%"),
        ).fetchone()
        require(row is not None, "Attendance row not found in database")
        require(row[1] is not None, "Check-out time not persisted in database")
        require(row[2] is not None and row[2] >= 1, "Work duration not persisted in database")
        require(row[3] == "Present", "Attendance status should be Present")
        record("DB attendance row persisted",
               f"check-in={row[0][:19]}, check-out={row[1][:19]}, duration={row[2]}m")

        row = cur.execute(
            "SELECT Name, IsActive FROM Departments WHERE Id = ?", (dept_id,)
        ).fetchone()
        require(row is not None and row[1] == 1, "Department row not found or inactive")
        record("DB department row persisted", f"{row[0]} (active={row[1]})")
    finally:
        con.close()

    # ------------------------------------------------------------------
    print("\n" + "=" * 72)
    print(f"ALL {len(results)} INTEGRATION CHECKS PASSED")
    print("=" * 72)
    print(f"Test data left in DB (unique run id {RUN_ID}):")
    print(f"  Department: {DEPT_NAME} (ID {dept_id})")
    print(f"  Employee  : {EMPLOYEE_EMAIL} (ID {employee_id})")
    print(f"  Leave     : ID {leave_id} (Approved)")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except AssertionError as exc:
        print("\n" + "=" * 72)
        print("INTEGRATION TEST FAILED")
        print("=" * 72)
        print(f"Reason: {exc}")
        print(f"Passed before failure: {len(results)}")
        sys.exit(1)
    except urllib.error.URLError as exc:
        print("\n" + "=" * 72)
        print("INTEGRATION TEST FAILED - cannot reach API")
        print("=" * 72)
        print(f"Reason: {exc}")
        print("Start the backend first: dotnet run --project backend/EmployeeManagementSystem.API")
        sys.exit(1)
