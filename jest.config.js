module.exports = {
    preset: "jest-puppeteer",
    globals: {
        RootUrl: "https://localhost/tallyj",
        Output: "JsTests/Output",
    },
    testMatch: ["**/JsTests/**/*.jest.js"],
    verbose: true
};