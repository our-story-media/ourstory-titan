#!/bin/bash -e

install -m 644 files/splash.service "${ROOTFS_DIR}/etc/systemd/system/splashscreen.service"

install -m 644 files/indaba.service "${ROOTFS_DIR}/etc/systemd/system/indaba.service"

install -m 644 files/indaba-supervisor.service "${ROOTFS_DIR}/etc/systemd/system/indaba-supervisor.service"

mkdir -p "${ROOTFS_DIR}/indaba"

install -m 644 files/indaba-update.tar "${ROOTFS_DIR}/indaba/"

install -m 774 files/indaba-supervisor "${ROOTFS_DIR}/indaba/"

install -m 644 files/splash.png "${ROOTFS_DIR}/opt/splash.png"

chmod +x "${ROOTFS_DIR}/indaba/indaba-supervisor"

install -m 774 files/gettitan "${ROOTFS_DIR}/indaba/"

chmod +x "${ROOTFS_DIR}/indaba/gettitan"

echo "indaba" > ${ROOTFS_DIR}/etc/hostname

echo "127.0.1.1    indaba" >> ${ROOTFS_DIR}/etc/hosts

on_chroot <<EOF

# SSL Workaround for no time
echo "-k" > ~/.curlrc

# Install docker
curl -sSL https://get.docker.com/ | sh

# update user group
usermod -aG docker indaba

touch /indaba/.supervisorinstalled

# disable splash
sed -i -e "$ i\disable_splash=1" /boot/config.txt

# update for automount USB
sed -i "s/PrivateMounts=.*/PrivateMounts=no/g" /lib/systemd/system/systemd-udevd.service

# install framebuffer image viewer & automount usb
apt install fbi usbmount

#enable indaba
systemctl enable indaba

#enable supervisor
systemctl enable indaba-supervisor

#enable splash
systemctl enable splashscreen

# disable console on boot
echo "console=serial0,115200 console=tty3 root=ROOTDEV rootfstype=ext4 elevator=deadline fsck.repair=yes rootwait splash quiet plymouth.ignore-serial-consoles logo.nologo disable_overscan=1" > /boot/cmdline.txt
EOF