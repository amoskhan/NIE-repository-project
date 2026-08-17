/**
 * Test Users
 * Manages test user configuration for testing
 */

export interface TestUser {
  username: string;
  password: string;
  name?: string;
  email?: string;
}

/**
 * Get the default test user from environment variables
 */
export function getTestUser(): TestUser {
  return {
    username: process.env.TEST_USERNAME || "",
    password: process.env.TEST_PASSWORD || "",
    name: process.env.TEST_USER_NAME || "Test User",
    email: process.env.TEST_USER_EMAIL || "",
  };
}

/**
 * Parse test user from environment variable
 * Format: username:password
 */
function parseTestUser(envValue: string | undefined): TestUser | null {
  if (!envValue) return null;
  const parts = envValue.split(":");
  if (parts.length < 2) return null;
  return {
    username: parts[0],
    password: parts[1],
  };
}

/**
 * Load all test users from environment variables
 * Supports TEST_USER_1 through TEST_USER_100
 */
export function loadTestUsers(): TestUser[] {
  const users: TestUser[] = [];

  // Try to load numbered test users
  for (let i = 1; i <= 100; i++) {
    const user = parseTestUser(process.env[`TEST_USER_${i}`]);
    if (user) {
      users.push(user);
    }
  }

  // If no numbered users, use default test user
  if (users.length === 0) {
    const defaultUser = getTestUser();
    if (defaultUser.username && defaultUser.password) {
      users.push(defaultUser);
    }
  }

  return users;
}

/**
 * Get a random test user from the pool
 */
export function getRandomTestUser(): TestUser | null {
  const users = loadTestUsers();
  if (users.length === 0) return null;
  return users[Math.floor(Math.random() * users.length)];
}

/**
 * Get a test user by index (with wraparound)
 */
export function getTestUserByIndex(index: number): TestUser | null {
  const users = loadTestUsers();
  if (users.length === 0) return null;
  return users[index % users.length];
}

/**
 * Check if test users are configured
 */
export function hasTestUsers(): boolean {
  const user = getTestUser();
  return !!(user.username && user.password);
}

export default { getTestUser, loadTestUsers, hasTestUsers };
