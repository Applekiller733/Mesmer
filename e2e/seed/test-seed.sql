
UPDATE "Accounts"
SET "Verified" = NOW(),
    "VerificationToken" = NULL,
    "Role" = 1
WHERE "Email" = 'e2e.user@mesmer.test';

-- Admin user.
UPDATE "Accounts"
SET "Verified" = NOW(),
    "VerificationToken" = NULL,
    "Role" = 0
WHERE "Email" = 'e2e.admin@mesmer.test';

-- Show the result so you can confirm both rows were updated.
SELECT "Email", "Role", "Verified" IS NOT NULL AS verified
FROM "Accounts"
WHERE "Email" IN ('e2e.user@mesmer.test', 'e2e.admin@mesmer.test');
