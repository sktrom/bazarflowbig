# Release Checklist

Verify the following before making a final production release:

- [ ] `git status` clean
- [ ] backend build successful
- [ ] backend unit tests passed
- [ ] backend integration tests passed
- [ ] frontend build successful
- [ ] frontend tests passed
- [ ] database migration applied successfully
- [ ] admin login verified
- [ ] cashier flow verified
- [ ] inventory deduction verified
- [ ] exports verified
- [ ] no console critical errors
- [ ] production environment values reviewed
- [ ] default admin password changed before real production
- [ ] no real database connection string is stored in tracked files
- [ ] production `ConnectionStrings__DefaultConnection` environment variable configured
- [ ] production SQL login does not use `sa`
- [ ] SQL password rotated after any accidental exposure
- [ ] SQL encryption settings reviewed for the deployment environment
