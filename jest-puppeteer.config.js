module.exports = {
    launch: {
        headless: false, //process.env.HEADLESS !== "false",
        slowMo: process.env.SLOWMO ? process.env.SLOWMO : 0,
        devtools: false,
        args: [
            '--window-size=1200,1000'
        ]
    }
};