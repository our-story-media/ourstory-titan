const drivelist = require('drivelist');
const _ = require('lodash');
const util = require('util');
const exec = util.promisify(require('child_process').exec);
const fs = require('fs');
const path = require('path');

console.log("Starting...")
let numdrives = -1;
let alreadyprocessing = false;

async function runExec(cmd) {
    const { stdout, stderr } = await exec(cmd, {cwd:'/home/pi'});
    console.log(stdout);
    console.error(stderr);
  }

async function update(pathin) {
    console.log("Performing Update...");

    let filename = path.join(pathin, 'indaba-update.tar');
    // let updatefile = fs.readdirSync(path);
    // console.log(updatefile);
    if (fs.existsSync(filename)) {

        console.log("Stopping Current Container");

        try {
            await runExec("docker stop indaba");
        }
        catch (e) {
            console.log(e);
        }

        console.log("Loading New Image");

        await runExec(`docker load --input "${filename}"`);

        console.log("Removing Old Image");

        try {
            await runExec("docker rm indaba");
        }
        catch (e) {
            console.log(e);
        }

        console.log("Removing Install Marker");

        try {
            await runExec("rm .titaninstalled");
        }
        catch (e) {
            console.log(e);
        }

        console.log("Run Install Script to Complete Update");

        await runExec("./gettitan");
    }
    else {
        console.error('No indaba-update.tar file!');
    }
}

async function start() {

    try {

        let drives = await drivelist.list();

        if (numdrives == -1)
            numdrives = _.size(drives);

        if (_.size(drives) > numdrives && !alreadyprocessing) {
            alreadyprocessing = true;
            console.log('New Drive Detected');

            let usb = _.find(drives, { isUSB: true });

            console.log(usb);
            numdrives = _.size(drives);

            //run update
            await update(usb.mountpoints[0].path);

            alreadyprocessing = false;
            setTimeout(start, 5000);
        }
        else {
            numdrives = _.size(drives);
            setTimeout(start, 5000);
        }
    }
    catch (e) {
        console.log("UPDATE FAILED!");
        console.error(e);
        setTimeout(start, 5000);
    }
}

start();