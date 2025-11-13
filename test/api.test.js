import { describe, it, before, after } from "mocha";
import { faker } from "@faker-js/faker";
import { strict as assert } from "assert";
import Ajv from "ajv";
import axios from "axios";
import fs from "fs";

const BASE_URL = "http://localhost:3000/api";
const CONTEXT_FILE = "test/context.json";

axios.defaults.validateStatus = (status) => status < 500;
axios.defaults.baseURL = BASE_URL;

const ajv = new Ajv({ allErrors: true });

const context = {};

before(() => {
  if (fs.existsSync(CONTEXT_FILE)) {
    Object.assign(context, JSON.parse(fs.readFileSync(CONTEXT_FILE, "utf-8")));
  }

  // Create test user if not exists
  if (!context.user) {
    context.user = generateTestUserData();
  }
});

after(() => {
  fs.writeFileSync(CONTEXT_FILE, JSON.stringify(context, null, 2));
});

describe("Auth", () => {
  it("Register", async () => {
    context.user = generateTestUserData();
    const res = await axios.post("/users", { user: context.user });
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().authenticatedUser);
    assert.equal(res.data.user.username, context.user.username);
    assert.equal(res.data.user.email, context.user.email);
    context.user.token = res.data.user.token;
  });

  it("Register - Bad request", async () => {
    const res = await axios.post("/users", {
      user: { email: context.user.email, password: context.user.password },
    });
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: ["#/user.username: PropertyRequired"],
    });
  });

  it("Register - Empty request", async () => {
    const res = await axios.post("/users", {});
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, { errors: ["#/user: PropertyRequired"] });
  });

  it("Login", async () => {
    const res = await axios.post("/users/login", {
      user: {
        email: context.user.email,
        password: context.user.password,
      },
    });
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().authenticatedUser);
    assert.equal(res.data.user.username, context.user.username);
    assert.equal(res.data.user.email, context.user.email);
    context.user.token = res.data.user.token;
  });

  it("Login - Bad email", async () => {
    const res = await axios.post("/users/login", {
      user: {
        email: faker.internet.email(),
        password: context.user.password,
      },
    });
    assert.equal(res.status, 401);
  });

  it("Login - Bad password", async () => {
    const res = await axios.post("/users/login", {
      user: {
        email: context.user.email,
        password: "wrongpassword",
      },
    });
    assert.equal(res.status, 401);
  });
});

describe("User", () => {
  it("Current user", async () => {
    const res = await axios.get("/user", {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().authenticatedUser);
    assert.equal(res.data.user.username, context.user.username);
    assert.equal(res.data.user.email, context.user.email);
  });

  it("Current user - Bad token", async () => {
    const res = await axios.get("/user", {
      headers: { Authorization: "BadToken" },
    });
    assert.equal(res.status, 401);
  });

  it("Current user - Missing token", async () => {
    const res = await axios.get("/user");
    assert.equal(res.status, 401);
  });

  it("Update user", async () => {
    const newBio = faker.lorem.sentence();
    const newImage = faker.image.avatar();
    const res = await axios.put(
      "/user",
      { user: { bio: newBio, image: newImage } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().authenticatedUser);
    assert.equal(res.data.user.bio, newBio);
    assert.equal(res.data.user.image, newImage);
    context.user = res.data.user;
  });

  it("Update user - Bad request", async () => {
    const res = await axios.put(
      "/user",
      { xuser: { email: "not-an-email" } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: [
        "#/user: PropertyRequired",
        "#/xuser: NoAdditionalPropertiesAllowed",
      ],
    });
  });

  it("Update user - No mutations", async () => {
    const res = await axios.put(
      "/user",
      { user: {} },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: { user: ["At least one field must be updated"] },
    });
  });
});

describe("Profile", () => {
  it("Get profile", async () => {
    // Register celeb user
    if (!context.celebUser) {
      const celebUserData = generateTestUserData("celeb_");
      const res = await axios.post("/users", {
        user: celebUserData,
      });
      assert.equal(res.status, 200);
      context.celebUser = res.data.user;
    }

    const res = await axios.get(`/profiles/${context.celebUser.username}`);
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.bio, "");
    assert.equal(res.data.profile.image, "");
    assert.equal(res.data.profile.following, false);
  });

  it("Get profile - Unknown", async () => {
    const res = await axios.get(`/profiles/${faker.internet.username()}`);
    assert.equal(res.status, 404);
  });

  it("Follow", async () => {
    const res = await axios.post(
      `/profiles/${context.celebUser.username}/follow`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.following, true);
  });

  it("Follow again", async () => {
    const res = await axios.post(
      `/profiles/${context.celebUser.username}/follow`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.following, true);
  });

  it("Profile after follow", async () => {
    const res = await axios.get(`/profiles/${context.celebUser.username}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.following, true);
  });

  it("Follow unknown", async () => {
    const res = await axios.post(
      `/profiles/${faker.internet.username()}/follow`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });
});

// ----------------------------------------
// HELPERS
// ----------------------------------------
function generateTestUserData(prefix = "") {
  const username = prefix + faker.internet.username().toLowerCase();
  const email = `${username}@email.com`;
  const password = "password";
  return { username, email, password };
}

function assertSchema(data, schema) {
  const validate = ajv.compile(schema);
  const valid = validate(data);
  if (!valid) {
    console.error(validate.errors);
  }
  assert.ok(valid, "Response does not match schema");
}

function getSchemas() {
  return {
    authenticatedUser: {
      type: "object",
      properties: {
        user: {
          type: "object",
          properties: {
            email: {
              type: "string",
            },
            token: {
              type: "string",
            },
            username: {
              type: "string",
            },
            bio: {
              type: ["string", "null"],
            },
            image: {
              type: ["string", "null"],
            },
          },
          additionalProperties: false,
          required: ["email", "token", "username"],
        },
      },
      additionalProperties: false,
      required: ["user"],
    },
    profile: {
      type: "object",
      properties: {
        profile: {
          type: "object",
          properties: {
            username: {
              type: "string",
            },
            bio: {
              type: "string",
            },
            image: {
              type: "string",
            },
            following: {
              type: "boolean",
            },
          },
          required: ["username", "bio", "image", "following"],
        },
      },
      required: ["profile"],
    },
    article: {
      type: "object",
      properties: {
        article: {
          type: "object",
          properties: {
            slug: { type: "string" },
            title: { type: "string" },
            description: { type: "string" },
            body: { type: "string" },
            tagList: {
              type: "array",
              items: { type: "string" },
            },
            createdAt: {
              type: "string",
              format: "date-time",
            },
            updatedAt: {
              type: "string",
              format: "date-time",
            },
            favorited: { type: "boolean" },
            favoritesCount: { type: "integer" },
            author: {
              type: "object",
              properties: {
                username: { type: "string" },
                bio: { type: "string" },
                image: { type: "string" },
                following: { type: "boolean" },
              },
              required: ["username", "bio", "image", "following"],
              additionalProperties: false,
            },
          },
          required: [
            "slug",
            "title",
            "description",
            "body",
            "tagList",
            "createdAt",
            "updatedAt",
            "favorited",
            "favoritesCount",
            "author",
          ],
          additionalProperties: false,
        },
      },
      required: ["article"],
      additionalProperties: false,
    },
  };
}
