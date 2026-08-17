// PROJECT-OWNED — safe to edit.
//
// Admin-side user administration, backed entirely by AccessControlController on the
// Main API. There is NO external staff directory in this template: accounts are created
// by self-registration (auth SPA → POST /api/Auth/Register) or by an administrator
// through register() below.
//
// The account table itself is GetUserAccounts — GetOverview only knows about logins that
// already hold a role assignment, and it has no account id at all. Everything here is
// keyed on the two identifiers the backend actually owns: UserAccount.Id (the numeric
// `id` ApproveUser/DeactivateUser take) and UserAccount.UserId (the login name that roles,
// sessions, and audit logs are written against). Never a synthetic row number.

import api from "./api";
import type {
  DeactivateUserResult,
  RegisterStaffRequest,
  StaffUser,
} from "@/types";

/** Wire shape of Shared.Dto.UserAccountDto. */
interface UserAccountResponse {
  id: number;
  userId: string;
  fullName?: string | null;
  email?: string | null;
  department?: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  lockoutEndOn?: string | null;
  lastLoginOn?: string | null;
}

/** Wire shape of Shared.Dto.DeactivateUserResultDto. */
interface DeactivateUserResponse {
  account: UserAccountResponse;
  sessionsRevoked: number | null;
  warning: string | null;
}

function isLockedOut(account: UserAccountResponse): boolean {
  return (
    !!account.lockoutEndOn &&
    new Date(account.lockoutEndOn).getTime() > Date.now()
  );
}

function toStaffUser(account: UserAccountResponse): StaffUser {
  return {
    id: account.id,
    username: account.userId,
    email: account.email ?? null,
    fullName: account.fullName ?? null,
    department: account.department ?? null,
    accountStatus: account.isActive
      ? isLockedOut(account)
        ? "Locked"
        : "Active"
      : "Inactive",
    isApproved: account.isActive,
    mustChangePassword: account.mustChangePassword,
    lockoutEndOn: account.lockoutEndOn ?? null,
    lastLoginAt: account.lastLoginOn ?? null,
  };
}

const staffUserService = {
  async getAll(): Promise<StaffUser[]> {
    const accounts = (
      await api.get<UserAccountResponse[]>("/api/AccessControl/GetUserAccounts")
    ).data;

    return accounts.map(toStaffUser);
  },

  async register(request: RegisterStaffRequest): Promise<StaffUser> {
    const account = (
      await api.post<UserAccountResponse>(
        "/api/AccessControl/RegisterUser",
        request,
      )
    ).data;

    return toStaffUser(account);
  },

  // ApproveUser and DeactivateUser bind `id` [FromQuery], so it goes on the query string.
  // A POSTed body binds to nothing there and leaves id at 0.
  async approve(id: number): Promise<StaffUser> {
    const account = (
      await api.post<UserAccountResponse>(
        `/api/AccessControl/ApproveUser?id=${id}`,
      )
    ).data;

    return toStaffUser(account);
  },

  async deactivate(id: number): Promise<DeactivateUserResult> {
    const result = (
      await api.post<DeactivateUserResponse>(
        `/api/AccessControl/DeactivateUser?id=${id}`,
      )
    ).data;

    return {
      account: toStaffUser(result.account),
      // null means the session store was unreachable: the user may still be signed in
      // somewhere, so the caller has to say so rather than report a clean deactivation.
      sessionsRevoked: result.sessionsRevoked ?? null,
      warning: result.warning ?? null,
    };
  },
};

export default staffUserService;
