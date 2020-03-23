const timeout = process.env.SLOWMO ? 30000 : 10000;

const testIdPw = 'test1234!';
const testId = 'test1';

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

    test('Login', async() => {
        await page.click('#btnChooseLogin');

        await page.type('#UserName', testId);
        await page.type('#PasswordV1', testIdPw);

        await Promise.all([
            page.waitForNavigation(),
            page.click('.LoginPanel input[type="submit"]')
        ]);

    }, timeout);

    test('Main buttons', async() => {

    }, timeout);
});

describe("electionPage", () => {

    test('Title', async() => {
        const title = await page.title();
        expect(title).toBe('TallyJ - Elections');
    }, timeout);

    test('Find Test 1 Election', async() => {
        const electionName = await page.$eval('#el-0e972f90-687d-4f5f-a25c-8c2b285594e3 .Detail b', el => el.innerText);
        expect(electionName).toBe('Election 1');

        await Promise.all([
            page.waitForNavigation(),
            page.click('#el-0e972f90-687d-4f5f-a25c-8c2b285594e3 button.btnSelectElection')
        ]);
    }, timeout);

});

describe("Dashboard", () => {

    test('Title', async() => {
        const title = await page.title();
        expect(title).toBe('TallyJ - Dashboard');
    }, timeout);

});



describe("Settings Up - Configure", () => {

    test('In Setup Mode', async() => {
        let sectionTitle = await page.$eval('#qmenuTitle', el => el.innerText);

        if (sectionTitle !== 'Setting Up') {
            await page.hover('.state.NotStarted');
            await page.click('#setThisNotStarted');
            await page.waitFor(500);
        }

        let sectionTitle2 = await page.$eval('#qmenuTitle', el => el.innerText);
        expect(sectionTitle2).toBe('Setting Up');

    }, timeout);

    test('On Setup Page', async() => {

        await Promise.all([
            page.waitForNavigation(),
            page.click('#menuNotStarted > a[href="/TallyJ/Setup"]')
        ]);

        const title = await page.title();
        expect(title).toBe('TallyJ - Election Setup');
    }, timeout);

});


describe("Dashboard - Settings Up - Configure", () => {

    test('In Setup Mode', async() => {
        const electionName = await page.$eval('#qmenuTitle', el => el.innerText);
        expect(electionName).toBe('Setting Up');
    }, timeout);

    test('On Setup Page', async() => {

        await Promise.all([
            page.waitForNavigation(),
            page.click('#menuNotStarted > a[href="/TallyJ/Setup"]')
        ]);

        const title = await page.title();
        expect(title).toBe('TallyJ - Election Setup');
    }, timeout);

    test('On Names Page', async() => {

        await Promise.all([
            page.waitForNavigation(),
            page.click('#menuNotStarted > a[href="/TallyJ/Setup/People"]')
        ]);

        const title = await page.title();
        expect(title).toBe('TallyJ - People');
    }, timeout);

    test('Find Person', async() => {

        await page.waitForSelector('#more:not(:empty)');

        const msg = await page.$eval('#more', el => el.innerText);
        expect(msg).toBe('10 people on file');

        await page.waitFor(500);

        await page.type('#txtSearch', "First", {
            delay: 100
        });

        // const numMatches = await page.$$eval('#nameList li', liList => liList.length);
        // expect(numMatches).toBe(10);
        // await page.waitFor(2000);

        // page.click('li.selected');
        await page.click('li#P849574');

        await page.waitForSelector('#btnSave', {
            visible: true
        });

        // await page.waitFor(50000);

        const firstName = await page.$eval('.personEdit input', el => el.value);
        expect(firstName).toBe('First1');

    }, timeout);

});



describe("Gathering Ballots", () => {

    test('In Gathering Ballots Mode', async() => {
        await page.hover('.state.NamesReady')
        await page.click('#setThisNamesReady');

        await page.waitFor(500);

        const sectionTitle = await page.$eval('#qmenuTitle', el => el.innerText);
        expect(sectionTitle).toBe('Gathering Ballots');

    }, timeout);

    test('On Front Desk', async() => {

        await Promise.all([
            page.waitForNavigation(),
            page.goto('https://localhost/TallyJ/Before/FrontDesk')
        ]);

        const title = await page.title();
        expect(title).toBe('TallyJ - Front Desk');
    }, timeout);

});
















describe("Finish up", () => {

    test('Log out', async() => {
        await page.goto('https://localhost/TallyJ/Account/Logoff', {
            waitUntil: 'domcontentloaded'
        });
    }, timeout);

});