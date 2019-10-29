if [ -d "pi-gen" ]; then
    echo "pi-gen already cloned"
else
    git clone https://github.com/RPi-Distro/pi-gen.git
fi

sudo apt-get install -f quilt qemu-user-static debootstrap bsdtar

cp config ./pi-gen/config

cp ../supervisor/build/indaba-supervisor ./pi-gen/stage2/01-sys-tweaks/files

cp ../gettitan ./pi-gen/stage2/01-sys-tweaks/files

cp splash.png ./pi-gen/stage2/01-sys-tweaks/files

cp indaba.service ./pi-gen/stage2/01-sys-tweaks/files

cp indaba-supervisor.service ./pi-gen/stage2/01-sys-tweaks/files

cp splash.service ./pi-gen/stage2/01-sys-tweaks/files

cp 02-run.sh ./pi-gen/stage2/01-sys-tweaks/

chmod +x ./pi-gen/stage2/01-sys-tweaks/02-run.sh

rm ./pi-gen/stage2/EXPORT_NOOBS

# if [ -f "./pi-gen/stage2/01-sys-tweaks/files/indaba-update.tar" ]; then
#   echo "tar already exists"
# else

#   docker pull bootlegger/titan-compact:latest

#   docker save -o ./pi-gen/stage2/01-sys-tweaks/files/indaba-update.tar bootlegger/titan-compact:latest

# fi

touch ./pi-gen/stage3/SKIP ./pi-gen/stage4/SKIP ./pi-gen/stage5/SKIP
touch ./pi-gen/stage4/SKIP_IMAGES ./pi-gen/stage5/SKIP_IMAGES

cd pi-gen
# docker-compose up -d

# Uncomment the following line to speed up building
# touch ./stage0/SKIP ./stage1/SKIP

sudo ./build.sh

sleep 1

echo "Final file is located at " `ls -Art ./deploy/*.zip | tail -n 1`