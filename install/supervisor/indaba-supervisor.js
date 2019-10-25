require('console-stamp')(console);
const drivelist = require('drivelist');
const _ = require('lodash');
const util = require('util');
const exec = util.promisify(require('child_process').exec);
const fs = require('fs');
const mkdir = util.promisify(require('fs').mkdir);
const cp = util.promisify(require('fs').copyFile);
const path = require('path');

console.log("Starting...")
let numdrives = -1;
let alreadyprocessing = false;

function runExec(cmd) {
    const exec = require('child_process').exec;
    return new Promise((resolve, reject) => {
        exec(cmd, (error, stdout, stderr) => {
            console.log(stdout);
            console.error(stderr);
            if (error) {
                console.warn(error);
            }
            resolve(stdout ? stdout : stderr);
        });
    });
}

async function update(pathin) {

    console.log("Backup Logs...");

    let logdir = `logs-${Math.floor(Date.now() / 1000)}`;

    console.log(`Creating ${logdir}`);

    try {
        await mkdir(`${path.join(pathin, 'indaba-logs')}`);
    }
    catch (e) {
        console.error(e);
    }

    try {
        await mkdir(`${path.join(pathin, 'indaba-logs', logdir)}`);
    }
    catch (e) {
        console.error(e);
    }

    try {
        await runExec(`cp /indaba/*.log "${path.join(pathin, 'indaba-logs', logdir)}"`);
    }
    catch (e) {
        console.error(e);
    }

    try {
        await runExec(`docker logs indaba > "${path.join(pathin, 'indaba-logs', `${logdir}/docker.log`)}"`);
    }
    catch (e) {
        console.error("Failed to Write Logs");
        console.error(e);
    }

    console.log("Performing Update...");

    let filename = path.join(pathin, 'indaba-update.tar');

    if (fs.existsSync(filename)) {

        console.log("Stopping Current Container");

        try {
            await runExec("docker stop indaba");
        }
        catch (e) {
            console.error(e);
        }

        console.log("Loading New Image");

        await runExec(`docker load --input "${filename}"`);

        console.log("Removing Old Image");

        try {
            await runExec("docker rm indaba");
        }
        catch (e) {
            console.error(e);
        }

        console.log("Removing Install Marker");

        try {
            await runExec("rm .titaninstalled");
        }
        catch (e) {
            console.error(e);
        }

        console.log("Run Install Script to Complete Update");

        await runExec("./gettitan");
    }
    else {
        console.error('No indaba-update.tar file!');
    }
}

function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }  

async function start() {

    // console.log(process.env);

    try {
        let drives = await drivelist.list();

        if (numdrives == -1)
            numdrives = _.size(drives);

        if (_.size(drives) > numdrives && !alreadyprocessing) {
            alreadyprocessing = true;
            console.log('New Drive Detected');

            // await sleep(5000);

            // drives = await drivelist.list();

            let usb = _.find(drives, { isUSB: true });

            // console.log(usb);

            numdrives = _.size(drives);

            if (usb.mountpoints.length > 0) {
                //run update
                console.log("Running update from", usb.mountpoints[0].path);
                await update(usb.mountpoints[0].path);
            }
            else {
                console.log("No Mountpoint", usb);
            }

            alreadyprocessing = false;
        }
        else {
            numdrives = _.size(drives);
        }

        setTimeout(start, 5000);
    }
    catch (e) {
        console.error("UPDATE FAILED!");
        console.error(e);
        setTimeout(start, 5000);
    }
}

start();