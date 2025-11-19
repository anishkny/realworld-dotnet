import { describe, it, before, after } from "mocha";
import { faker } from "@faker-js/faker";
import { strict as assert } from "assert";
import Ajv from "ajv";
import addFormats from "ajv-formats";
import axios from "axios";
import fs from "fs";

const BASE_URL = "http://localhost:3000/api";
const CONTEXT_FILE = "test/context.json";

axios.defaults.validateStatus = (status) => status < 500;
axios.defaults.baseURL = BASE_URL;

const ajv = new Ajv({ allErrors: true });
addFormats(ajv);

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

describe("Health", () => {
  it("Root", async () => {
    const res = await axios.get("");
    assert.equal(res.status, 200);
  });
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
      user: { email: context.user.email },
    });
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: [
        "#/user.username: PropertyRequired",
        "#/user.password: PropertyRequired",
      ],
    });

    // Empty request
    const res2 = await axios.post("/users", null);
    assert.equal(res2.status, 422);
    assert.deepEqual(res2.data, { errors: ["Error parsing JSON"] });
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
    const newEmail = "updated_" + context.user.email;
    const newBio = faker.lorem.sentence();
    const newImage = faker.image.avatar();
    const res = await axios.put(
      "/user",
      { user: { email: newEmail, bio: newBio, image: newImage } },
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
    const celebUserData = generateTestUserData("celeb_");
    const res = await axios.post("/users", {
      user: celebUserData,
    });
    assert.equal(res.status, 200);
    context.celebUser = res.data.user;

    const res2 = await axios.get(`/profiles/${context.celebUser.username}`);
    assert.equal(res2.status, 200);
    assertSchema(res2.data, getSchemas().profile);
    assert.equal(res2.data.profile.username, context.celebUser.username);
    assert.equal(res2.data.profile.bio, "");
    assert.equal(res2.data.profile.image, "");
    assert.equal(res2.data.profile.following, false);
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

  it("Unfollow", async () => {
    const res = await axios.delete(
      `/profiles/${context.celebUser.username}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.following, false);
  });

  it("Unfollow again", async () => {
    const res = await axios.delete(
      `/profiles/${context.celebUser.username}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().profile);
    assert.equal(res.data.profile.username, context.celebUser.username);
    assert.equal(res.data.profile.following, false);
  });

  it("Unfollow unknown", async () => {
    const res = await axios.delete(
      `/profiles/${faker.internet.username()}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });
});

describe("Articles", () => {
  it("Create article", async () => {
    context.article = {
      title: "Test Article " + faker.lorem.sentence(),
      description: faker.lorem.sentences(2),
      body: faker.lorem.paragraphs(3),
      tagList: [faker.lorem.word(), faker.lorem.word(), faker.lorem.word()],
    };
    const res = await axios.post(
      "/articles",
      { article: context.article },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().article);
    assert.equal(res.data.article.title, context.article.title);
    assert.equal(res.data.article.description, context.article.description);
    assert.equal(res.data.article.body, context.article.body);
    assert.deepEqual(
      res.data.article.tagList.sort(),
      context.article.tagList.sort()
    );
    context.article = res.data.article;
  });

  it("Get article", async () => {
    const res = await axios.get(`/articles/${context.article.slug}`);
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().article);
    assert.equal(res.data.article.title, context.article.title);
    assert.equal(res.data.article.description, context.article.description);
    assert.equal(res.data.article.body, context.article.body);
    assert.deepEqual(
      res.data.article.tagList.sort(),
      context.article.tagList.sort()
    );
  });

  it("Get article - Unknown slug", async () => {
    const res = await axios.get(
      `/articles/unknown-slug-${faker.string.uuid()}`
    );
    assert.equal(res.status, 404);
  });

  it("Create article - Bad request", async () => {
    const res = await axios.post(
      "/articles",
      { xarticle: { title: "Invalid" } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: [
        "#/article: PropertyRequired",
        "#/xarticle: NoAdditionalPropertiesAllowed",
      ],
    });
  });

  it("Update article", async () => {
    const newTitle = "Updated Title " + faker.lorem.sentence();
    const newDescription = "Updated Description " + faker.lorem.sentences(2);
    const newBody = "Updated Body " + faker.lorem.paragraphs(3);
    const res = await axios.put(
      `/articles/${context.article.slug}`,
      {
        article: {
          title: newTitle,
          description: newDescription,
          body: newBody,
        },
      },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().article);
    assert.equal(res.data.article.title, newTitle);
    assert.equal(res.data.article.description, newDescription);
    assert.equal(res.data.article.body, newBody);
    assert.notEqual(res.data.article.slug, context.article.slug);
    assert(
      new Date(res.data.article.updatedAt) >
        new Date(res.data.article.createdAt)
    );
    context.article = res.data.article;
  });

  it("Update article - Tags", async () => {
    const newTagList = [
      "new-tag-" + faker.lorem.word(),
      "new-tag-" + faker.lorem.word(),
    ];
    const res = await axios.put(
      `/articles/${context.article.slug}`,
      {
        article: {
          tagList: newTagList,
        },
      },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assertSchema(res.data, getSchemas().article);
    assert.deepEqual(res.data.article.tagList.sort(), newTagList.sort());
    context.article = res.data.article;
  });

  it("Update article - Bad request", async () => {
    const res = await axios.put(
      `/articles/${context.article.slug}`,
      { xarticle: { title: "Invalid" } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: [
        "#/article: PropertyRequired",
        "#/xarticle: NoAdditionalPropertiesAllowed",
      ],
    });
  });

  it("Update article - Unknown slug", async () => {
    const res = await axios.put(
      `/articles/unknown-slug-${faker.string.uuid()}`,
      {
        article: {
          title: "New Title",
        },
      },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Update article - Forbidden", async () => {
    const res = await axios.put(
      `/articles/${context.article.slug}`,
      { article: { title: "Hacked Title" } },
      { headers: { Authorization: context.celebUser.token } }
    );
    assert.equal(res.status, 403);
  });

  it("Delete article - Unknown slug", async () => {
    const res = await axios.delete(
      `/articles/unknown-slug-${faker.string.uuid()}`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Delete article - Forbidden", async () => {
    const res = await axios.delete(`/articles/${context.article.slug}`, {
      headers: { Authorization: context.celebUser.token },
    });
    assert.equal(res.status, 403);
  });

  it("Delete article", async () => {
    const res = await axios.delete(`/articles/${context.article.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res.status, 200);
  });

  it("Get article - Followed user", async () => {
    // Create new article by celeb user
    const newArticle = {
      title: "Celeb Article " + faker.lorem.sentence(),
      description: faker.lorem.sentences(2),
      body: faker.lorem.paragraphs(3),
      tagList: [faker.lorem.word(), faker.lorem.word()],
    };
    const res = await axios.post(
      "/articles",
      { article: newArticle },
      { headers: { Authorization: context.celebUser.token } }
    );
    assert.equal(res.status, 200);
    const celebArticle = res.data.article;
    context.celebArticle = celebArticle;

    // User unfollows celeb
    const res2 = await axios.delete(
      `/profiles/${context.celebUser.username}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res2.status, 200);

    // Get article and verify following is false
    const res3 = await axios.get(`/articles/${celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res3.status, 200);
    assertSchema(res3.data, getSchemas().article);
    assert.equal(res3.data.article.author.username, context.celebUser.username);
    assert.equal(res3.data.article.author.following, false);

    // User follows celeb
    const res4 = await axios.post(
      `/profiles/${context.celebUser.username}/follow`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res4.status, 200);

    // Get article and verify following is true
    const res5 = await axios.get(`/articles/${celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res5.status, 200);
    assertSchema(res5.data, getSchemas().article);
    assert.equal(res5.data.article.author.username, context.celebUser.username);
    assert.equal(res5.data.article.author.following, true);

    // Unfollow celeb to clean up
    const res6 = await axios.delete(
      `/profiles/${context.celebUser.username}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res6.status, 200);
  });
});

describe("Favorites", () => {
  it("Favorite article", async () => {
    // Create celeb article
    const newArticle = {
      title: "Celeb Article " + faker.lorem.sentence(),
      description: faker.lorem.sentences(2),
      body: faker.lorem.paragraphs(3),
      tagList: [faker.lorem.word(), faker.lorem.word()],
    };
    const resCreate = await axios.post(
      "/articles",
      { article: newArticle },
      { headers: { Authorization: context.celebUser.token } }
    );
    assert.equal(resCreate.status, 200);
    context.celebArticle = resCreate.data.article;

    // Favorite celeb article
    const res2 = await axios.post(
      `/articles/${context.celebArticle.slug}/favorite`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res2.status, 200);

    // Verify article is favorited
    const res3 = await axios.get(`/articles/${context.celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res3.status, 200);
    assertSchema(res3.data, getSchemas().article);
    assert.equal(res3.data.article.favorited, true);
    assert.equal(res3.data.article.favoritesCount, 1);

    // Verify favoritesCount as unauthenticated user
    const res4 = await axios.get(`/articles/${context.celebArticle.slug}`);
    assert.equal(res4.status, 200);
    assertSchema(res4.data, getSchemas().article);
    assert.equal(res4.data.article.favorited, false);
    assert.equal(res4.data.article.favoritesCount, 1);
  });

  it("Favorite - Again", async () => {
    // Favorite celeb article again
    const res = await axios.post(
      `/articles/${context.celebArticle.slug}/favorite`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);

    // Verify article is still favorited only once
    const res2 = await axios.get(`/articles/${context.celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res2.status, 200);
    assertSchema(res2.data, getSchemas().article);
    assert.equal(res2.data.article.favorited, true);
    assert.equal(res2.data.article.favoritesCount, 1);
  });

  it("Unfavorite article", async () => {
    // Unfavorite celeb article
    const res = await axios.delete(
      `/articles/${context.celebArticle.slug}/favorite`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);

    // Verify article is unfavorited
    const res2 = await axios.get(`/articles/${context.celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res2.status, 200);
    assertSchema(res2.data, getSchemas().article);
    assert.equal(res2.data.article.favorited, false);
    assert.equal(res2.data.article.favoritesCount, 0);
  });

  it("Unfavorite - Again", async () => {
    // Unfavorite celeb article again
    const res = await axios.delete(
      `/articles/${context.celebArticle.slug}/favorite`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);

    // Verify article is still unfavorited
    const res2 = await axios.get(`/articles/${context.celebArticle.slug}`, {
      headers: { Authorization: context.user.token },
    });
    assert.equal(res2.status, 200);
    assertSchema(res2.data, getSchemas().article);
    assert.equal(res2.data.article.favorited, false);
    assert.equal(res2.data.article.favoritesCount, 0);
  });

  it("Favorite - Unknown article", async () => {
    const res = await axios.post(
      `/articles/unknown-slug-${faker.string.uuid()}/favorite`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Unfavorite - Unknown article", async () => {
    const res = await axios.delete(
      `/articles/unknown-slug-${faker.string.uuid()}/favorite`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });
});

describe("Comments", () => {
  it("Create comment", async () => {
    // Create comment on celeb article
    const commentBody = faker.lorem.sentences(2);
    const res = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { comment: { body: commentBody } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
    assert.ok(res.data.comment);
    assert.equal(res.data.comment.body, commentBody);
    context.comment = res.data.comment;
  });

  it("Create comment - Bad request", async () => {
    const res = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { xcomment: { body: "Invalid" } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 422);
    assert.deepEqual(res.data, {
      errors: [
        "#/comment: PropertyRequired",
        "#/xcomment: NoAdditionalPropertiesAllowed",
      ],
    });
  });

  it("Create comment - Unknown article", async () => {
    const res = await axios.post(
      `/articles/unknown-slug-${faker.string.uuid()}/comments`,
      { comment: { body: "Test comment" } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Delete comment", async () => {
    const res = await axios.delete(
      `/articles/${context.celebArticle.slug}/comments/${context.comment.id}`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 200);
  });

  it("Delete comment - Unknown article", async () => {
    const res = await axios.delete(
      `/articles/unknown-slug-${faker.string.uuid()}/comments/${
        context.comment.id
      }`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Delete comment - Unknown comment", async () => {
    const res = await axios.delete(
      `/articles/${context.celebArticle.slug}/comments/${faker.string.uuid()}`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });

  it("Delete comment - Unauthorized", async () => {
    // Create comment to delete
    const commentBody = faker.lorem.sentences(2);
    const resCreate = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { comment: { body: commentBody } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(resCreate.status, 200);
    const commentToDelete = resCreate.data.comment;

    // Attempt to delete comment as celeb user
    const resDelete = await axios.delete(
      `/articles/${context.celebArticle.slug}/comments/${commentToDelete.id}`,
      { headers: { Authorization: context.celebUser.token } }
    );
    assert.equal(resDelete.status, 403);

    // Clean up by deleting comment as original user
    const resCleanup = await axios.delete(
      `/articles/${context.celebArticle.slug}/comments/${commentToDelete.id}`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(resCleanup.status, 200);
  });

  it("Get comments", async () => {
    // Create two comments
    const commentBody1 = faker.lorem.sentences(2);
    const res1 = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { comment: { body: commentBody1 } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res1.status, 200);

    const commentBody2 = faker.lorem.sentences(2);
    const res2 = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { comment: { body: commentBody2 } },
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res2.status, 200);

    // Create comment by celeb user
    const commentBody3 = faker.lorem.sentences(2);
    const res3 = await axios.post(
      `/articles/${context.celebArticle.slug}/comments`,
      { comment: { body: commentBody3 } },
      { headers: { Authorization: context.celebUser.token } }
    );
    assert.equal(res3.status, 200);

    // Follow celeb user
    const resFollow = await axios.post(
      `/profiles/${context.celebUser.username}/follow`,
      {},
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(resFollow.status, 200);

    // Get comments for article
    const resGet = await axios.get(
      `/articles/${context.celebArticle.slug}/comments`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(resGet.status, 200);
    assert.ok(Array.isArray(resGet.data.comments));
    assert.ok(resGet.data.comments.find((c) => c.id === res1.data.comment.id));
    assert.ok(resGet.data.comments.find((c) => c.id === res2.data.comment.id));
    assert.ok(resGet.data.comments.find((c) => c.id === res3.data.comment.id));

    // Verify following status
    assert.ok(
      resGet.data.comments.find((c) => c.id === res3.data.comment.id).author
        .following
    );

    // Get comments as unauthenticated user
    const resGetUnauth = await axios.get(
      `/articles/${context.celebArticle.slug}/comments`
    );
    assert.equal(resGetUnauth.status, 200);
    assert.ok(Array.isArray(resGetUnauth.data.comments));
    for (const comment of resGetUnauth.data.comments) {
      assert.equal(comment.author.following, false);
    }

    // Clean up
    for (const comment of resGet.data.comments) {
      const token =
        comment.author.username === context.user.username
          ? context.user.token
          : context.celebUser.token;
      const resDel = await axios.delete(
        `/articles/${context.celebArticle.slug}/comments/${comment.id}`,
        { headers: { Authorization: token } }
      );
      assert.equal(resDel.status, 200);
    }

    // Unfollow celeb user
    const resUnfollow = await axios.delete(
      `/profiles/${context.celebUser.username}/follow`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(resUnfollow.status, 200);
  });

  it("Get comments - Unknown article", async () => {
    const res = await axios.get(
      `/articles/unknown-slug-${faker.string.uuid()}/comments`,
      { headers: { Authorization: context.user.token } }
    );
    assert.equal(res.status, 404);
  });
});

describe("Articles - List/Feed", () => {
  // Create multiple articles and authors for testing
  before(async () => {
    const promises = [];

    // Create few authors
    context.authors = [];
    for (let i = 0; i < 5; i++) {
      const authorData = generateTestUserData(`author${i}_`);
      const p = axios.post("/users", { user: authorData }).then((res) => {
        assert.equal(res.status, 200);
        context.authors[i] = res.data.user;
      });
      promises.push(p);
    }
    await Promise.all(promises);

    // Create few articles
    context.articles = [];
    for (let i = 0; i < 25; i++) {
      const articleData = {
        title: `Article ${i} ` + faker.lorem.sentence(),
        description: faker.lorem.sentences(2),
        body: faker.lorem.paragraphs(3),
        tagList: [i % 2 === 0 ? "even" : "odd", "test"],
      };
      const p = axios
        .post(
          "/articles",
          { article: articleData },
          {
            headers: {
              Authorization: context.authors[i % context.authors.length].token,
            },
          }
        )
        .then((res) => {
          assert.equal(res.status, 200);
          context.articles[i] = res.data.article;
        });
      promises.push(p);
    }
    await Promise.all(promises);

    // Favorite some articles by first user
    for (let i = 0; i < context.articles.length; i += 3) {
      const article = context.articles[i];
      const p = axios
        .post(
          `/articles/${article.slug}/favorite`,
          {},
          { headers: { Authorization: context.authors[0].token } }
        )
        .then((res) => {
          assert.equal(res.status, 200);
        });
      promises.push(p);
    }
    await Promise.all(promises);

    // Get user0 to follow user2 and user4
    const resFollow2 = await axios.post(
      `/profiles/${context.authors[2].username}/follow`,
      {},
      { headers: { Authorization: context.authors[0].token } }
    );
    assert.equal(resFollow2.status, 200);

    const resFollow4 = await axios.post(
      `/profiles/${context.authors[4].username}/follow`,
      {},
      { headers: { Authorization: context.authors[0].token } }
    );
    assert.equal(resFollow4.status, 200);
  });

  describe("List Articles", () => {
    it("List articles", async () => {
      const res = await axios.get("/articles");
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assert.equal(res.data.articles.length, 20);
      assert.equal(res.data.articlesCount, 20);
      assertArticlesInDescendingOrder(res.data.articles);
    });

    it("List articles - Limit and Offset", async () => {
      const res = await axios.get("/articles?limit=10&offset=5");
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assert.equal(res.data.articles.length, 10);
      assert.equal(res.data.articlesCount, 10);
      assertArticlesInDescendingOrder(res.data.articles);
    });

    it("List articles - Tag", async () => {
      const res = await axios.get("/articles?tag=even");
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      for (const article of res.data.articles) {
        assert.ok(article.tagList.includes("even"));
      }
      assertArticlesInDescendingOrder(res.data.articles);
    });

    it("List articles - Author", async () => {
      const author = context.authors[0];
      const res = await axios.get(`/articles?author=${author.username}`);
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      for (const article of res.data.articles) {
        assert.equal(article.author.username, author.username);
      }
      assertArticlesInDescendingOrder(res.data.articles);
    });

    it("List articles - Favorited", async () => {
      const user = context.authors[0];
      const res = await axios.get(`/articles?favorited=${user.username}`);
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      for (const article of res.data.articles) {
        // Article should be favorited by the user
        const resArticle = await axios.get(`/articles/${article.slug}`, {
          headers: { Authorization: user.token },
        });
        assert.equal(resArticle.status, 200);
        assert.equal(resArticle.data.article.favorited, true);
      }
      assertArticlesInDescendingOrder(res.data.articles);
    });

    it("List articles - Authenticated", async () => {
      const user = context.authors[0];
      const res = await axios.get("/articles", {
        headers: { Authorization: user.token },
      });
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assertArticlesInDescendingOrder(res.data.articles);

      // Verify that following is true for authors 2 and 4, false for others
      for (const article of res.data.articles) {
        if (
          article.author.username === context.authors[2].username ||
          article.author.username === context.authors[4].username
        ) {
          assert.equal(article.author.following, true);
        } else {
          assert.equal(article.author.following, false);
        }
      }
    });
  });

  describe("Feed Articles", () => {
    it("Feed articles", async () => {
      const user = context.authors[0];
      const res = await axios.get("/articles/feed", {
        headers: { Authorization: user.token },
      });
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assertArticlesInDescendingOrder(res.data.articles);

      // All articles should be from followed users (authors 2 and 4)
      for (const article of res.data.articles) {
        assert.ok(
          article.author.username === context.authors[2].username ||
            article.author.username === context.authors[4].username
        );
      }
    });

    it("Feed articles - Limit and Offset", async () => {
      const user = context.authors[0];
      const res = await axios.get("/articles/feed?limit=5&offset=2", {
        headers: { Authorization: user.token },
      });
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assert.equal(res.data.articles.length, 5);
      assertArticlesInDescendingOrder(res.data.articles);

      // All articles should be from followed users (authors 2 and 4)
      for (const article of res.data.articles) {
        assert.ok(
          article.author.username === context.authors[2].username ||
            article.author.username === context.authors[4].username
        );
      }
    });

    it("Feed articles - No followed users", async () => {
      const user = context.authors[1]; // This user follows no one
      const res = await axios.get("/articles/feed", {
        headers: { Authorization: user.token },
      });
      assert.equal(res.status, 200);
      assert.ok(Array.isArray(res.data.articles));
      assert.equal(res.data.articles.length, 0);
      assert.equal(res.data.articlesCount, 0);
    });

    it("Feed articles - Unauthenticated", async () => {
      const res = await axios.get("/articles/feed");
      assert.equal(res.status, 401);
    });
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

function assertArticlesInDescendingOrder(articles) {
  for (let i = 1; i < articles.length; i++) {
    const prevDate = new Date(articles[i - 1].updatedAt);
    const currDate = new Date(articles[i].updatedAt);
    assert.ok(prevDate >= currDate);
  }
}
