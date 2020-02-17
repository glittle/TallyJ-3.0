const timeout = process.env.SLOWMO ? 30000 : 10000;

beforeAll(async() => {
    await page.setViewport({
        width: 1200,
        height: 900,
        deviceScaleFactor: 1,
    });
    await page.goto(RootUrl, {
        waitUntil: 'domcontentloaded'
    });
});

describe('Home Page Test Set 1', () => {
    test('Title of the page', async() => {
        const title = await page.title();
        expect(title).toBe('TallyJ - Bahá’í Election System');
    }, timeout);

    test('Header of the page', async() => {
        const versionDiv = await page.$('.Version');
        const html = await page.evaluate(h => h.innerText, versionDiv);

        expect(html).toEqual(expect.stringMatching(/^Version: 3.0 beta/));
    }, timeout);

    test('Main buttons', async() => {
        // const buttons = await page.$('.centerbuttons button');
        // expect(buttons.length).toBe(3);
    }, timeout);

    test('Main buttons', async() => {

    }, timeout);

    test('Main buttons', async() => {

    }, timeout);

    test('Main buttons', async() => {

    }, timeout);





});