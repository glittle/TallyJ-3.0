module.exports = {
    launch: {
        headless: false, //process.env.HEADLESS !== "false",
        slowMo: process.env.SLOWMO ? process.env.SLOWMO : 0,
        devtools: true,
        args: [
            '--window-size=1500,1000'
        ]
    }
};