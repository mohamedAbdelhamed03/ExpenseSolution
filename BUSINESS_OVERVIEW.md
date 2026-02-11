# Business Overview – Shared Expenses Management Platform

## 1. Business Overview
This system helps people manage shared spending with clarity and fairness. It solves the common problem of tracking who paid, who owes, and how much each person should contribute. It is designed for real‑life situations such as friends sharing a trip, roommates splitting rent and utilities, families planning events, or teams managing work expenses. The platform keeps everyone aligned, reduces misunderstandings, and makes it easy to settle up with confidence.

## 2. Target Users
**Who uses it**
- Friends planning trips or nights out
- Families sharing household or event costs
- Roommates splitting rent, utilities, and groceries
- Small teams tracking shared work expenses

**Goals**
- Track spending in a simple, transparent way
- Know exactly who owes whom
- Settle fairly without awkward disputes
- Understand where money is going over time

**Pain points before using the system**
- Messy spreadsheets and forgotten costs
- Conflicting records of who paid and who owes
- Unclear splits and unfair outcomes
- Too many back‑and‑forth payments

## 3. Core Business Concepts

### Groups
A group represents a set of people sharing expenses for a shared purpose (trip, home, project). Any member can create a group and invite others. Groups have roles:
- **Admins** manage membership and permissions.
- **Members** add and share expenses.

### Expenses
An expense is a recorded cost paid by one person on behalf of the group. Any group member can add expenses. Costs can be split:
- **Equally** across all members
- **Custom amounts** when shares differ
- Categories help organize spending (e.g., food, transport, utilities) to make reporting clearer.

### Personal Expenses
Users can also track their own private spending that is not shared with any group. This allows the platform to serve as a complete financial tracker, combining both shared and personal costs in one place. Only the user who created the personal expense can view it. Personal expenses can be categorized just like group expenses.

### Balances
A balance shows each person’s net position:
- **Positive balance** means the person is owed money.
- **Negative balance** means the person owes money.
Balances always reflect all recorded expenses and adjustments, so users can trust the numbers.

### Settlements
Settlements represent paying someone back. The system guides users to settle correctly and prevents mistakes such as over‑paying or paying the wrong person. This ensures that settlements always reduce debt fairly.

## 4. Debt Simplification (User Benefit)
To reduce the number of payments, the system simplifies debts automatically. For example, if Person A owes Person B, and Person B owes Person C, the system suggests that Person A pays Person C directly. This reduces unnecessary steps and makes settling faster and easier.

## 5. Unified Activity Feed
The Home Feed provides a single, chronological view of all financial activities relevant to the user. It aggregates:
- **Group Expenses**: Where the user paid or is involved in the split.
- **Settlements**: Payments made or received by the user.
- **Personal Expenses**: Private spending tracked by the user.
This unified view helps users stay on top of their latest financial interactions without navigating through multiple groups.

## 6. Insights & Analytics
Users can view spending summaries to understand behavior and trends:
- Total spending over a period
- Breakdown by category
- Monthly and yearly views
- Personal spending insights across all groups and personal expenses
These insights help users make better decisions, plan budgets, and identify where money is being spent most.

## 7. Notifications
Users receive notifications when important actions happen, such as:
- A new expense is added
- An expense is updated or removed
- A settlement is recorded
Notifications are delivered in **real-time** (via WebSockets) when users are online and are persisted so they can be viewed later. This ensures users never miss an update.

## 8. Data Accuracy & Trust
The system is designed to be reliable and trustworthy:
- All changes are recorded and traceable.
- Balances always reflect the latest approved data.
- If a user is offline, information is still available when they return.
- Records can be reviewed anytime, which builds confidence and avoids disputes.

## 9. Typical User Journey
1. A user creates a group for a trip.
2. They invite friends to join.
3. Members add expenses as they happen (meals, transport, lodging).
4. The group reviews balances to see who owes what.
5. Members settle debts based on suggested payments.
6. They review insights to understand where the money went.

## 10. Business Rules & Constraints
- Only group members can view and add expenses.
- Admins control member roles and removals.
- Expenses must have valid splits that add up correctly.
- Settlements cannot exceed what someone owes.
- Historical entries are preserved for accountability.

## 11. Non‑Goals
The system does not:
- Process payments
- Convert currencies
- Integrate with banks or financial institutions
- Automatically transfer money

---
This platform focuses on clarity, fairness, and trust—helping groups manage shared costs without conflict or confusion.
