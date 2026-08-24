import { expect, test, type Page, type Route } from "@playwright/test";

const MAIN_APP_URL =
  process.env.CHAT_FRONTEND_MAIN ?? "http://localhost:8002/";

test.use({ serviceWorkers: "block" });

const mockUser = {
  userId: "chat-actions-user",
  fullName: "Chat Actions User",
  email: "chat.actions@example.edu.sg",
  roles: ["SystemAdmin"],
  roleNames: ["System Administrator"],
  permissions: ["api.chat.use"],
};

const todayIso = new Date().toISOString();

const conversations = [
  {
    id: "019fc37a-71b9-7858-86f2-9fea26d10e34",
    title: "Budget review",
    userId: mockUser.userId,
    source: "procurement",
    lastMessageAt: todayIso,
    messageCount: 2,
  },
  {
    id: "019fc37a-71b9-7ff2-84c5-f7a5d698a116",
    title: "Vendor shortlist",
    userId: mockUser.userId,
    source: "procurement",
    lastMessageAt: "2026-05-25T02:00:00Z",
    messageCount: 0,
  },
];

async function fulfillJson(route: Route, body: unknown, status = 200) {
  await route.fulfill({
    status,
    contentType: "application/json",
    body: JSON.stringify(body),
  });
}

async function mockChatShell(page: Page) {
  let renamedTitle = conversations[0].title;
  let deletedConversationId: string | null = null;

  await page.context().addCookies([
    {
      name: "Application-SessionToken",
      value: "chat-actions-session",
      domain: "localhost",
      path: "/",
    },
    {
      name: "Application-User",
      value: JSON.stringify(mockUser),
      domain: "localhost",
      path: "/",
    },
  ]);

  await page.route("**/api-main/api/**", async (route) => {
    const request = route.request();
    const url = request.url();

    if (url.includes("/AccessControl/GetCurrentAccessProfile")) {
      await fulfillJson(route, {
        userId: mockUser.userId,
        roleCodes: mockUser.roles,
        roleNames: mockUser.roleNames,
        accessFunctionCodes: mockUser.permissions,
      });
      return;
    }

    if (url.includes("/Chat/quota")) {
      await fulfillJson(route, {
        conversationsToday: 1,
        conversationsDailyLimit: 20,
        tokensToday: 10,
        tokensDailyLimit: 10000,
        retentionDays: 30,
        warnings: [],
        conversationsExceeded: false,
        tokensExceeded: false,
      });
      return;
    }

    if (url.includes("/Chat/conversations/019fc37a-71b9-7858-86f2-9fea26d10e34/messages")) {
      await fulfillJson(route, [
        {
          id: "019fc37a-71b9-7255-9908-f50c815425eb",
          role: "user",
          content: "Can you summarize the budget?",
          createdAt: todayIso,
          conversationId: "019fc37a-71b9-7858-86f2-9fea26d10e34",
        },
      ]);
      return;
    }

    if (url.includes("/Chat/conversations/019fc37a-71b9-7858-86f2-9fea26d10e34/rename")) {
      const body = request.postDataJSON() as { title?: string };
      renamedTitle = body.title ?? renamedTitle;
      await route.fulfill({ status: 204 });
      return;
    }

    if (
      request.method() === "DELETE" &&
      url.includes("/Chat/conversations/019fc37a-71b9-7858-86f2-9fea26d10e34")
    ) {
      deletedConversationId = "019fc37a-71b9-7858-86f2-9fea26d10e34";
      await route.fulfill({ status: 204 });
      return;
    }

    if (url.includes("/Chat/conversations")) {
      await fulfillJson(
        route,
        conversations
          .filter((conversation) => conversation.id !== deletedConversationId)
          .map((conversation) => ({
            ...conversation,
            title:
              conversation.id === "019fc37a-71b9-7858-86f2-9fea26d10e34"
                ? renamedTitle
                : conversation.title,
          })),
      );
      return;
    }

    await fulfillJson(route, []);
  });
}

test.describe("chat conversation actions", () => {
  test("selects, renames, and deletes a conversation from the sidebar", async ({
    page,
  }) => {
    await mockChatShell(page);
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(`${MAIN_APP_URL}#/chat`);

    await page
      .getByRole("button", { name: "Budget review Today · 2 msgs" })
      .click();
    await expect(
      page.getByText("Can you summarize the budget?"),
    ).toBeVisible();

    await page.getByLabel("Open actions for Budget review").click();
    await page.getByRole("button", { name: "Rename" }).click();
    await page.locator(".edit-input").fill("Renamed budget review");
    await page.keyboard.press("Enter");

    await expect(page.getByRole("heading", { name: "Renamed budget review" })).toBeVisible();

    await page.getByLabel("Open actions for Renamed budget review").click();
    await page.getByRole("button", { name: "Delete" }).click();

    await expect(
      page.getByRole("button", {
        name: "Renamed budget review Today · 2 msgs",
      }),
    ).toHaveCount(0);
  });
});
