module.exports = {
    preset: "jest-puppeteer",
    globals: {
        RootUrl: "https://localhost/TallyJ",
        Output: "JsTests/Output",
    },
    testMatch: ["**/JsTests/**/*.jest.js"],
    verbose: true,
    // noStackTrace: true,
};