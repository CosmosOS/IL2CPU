#!/bin/sh

echo "::: Assembler to ELF"
nasm -g -f elf -o Kernel.obj -dELF_COMPILATION -O0 $1.asm

echo "::: Linking to ELF x86"
ld -m elf_i386 -Ttext 0x2000000 -Tdata 0x1000000 -e Kernel_Start -o Kernel.bin Kernel.obj

echo "::: Generating symbols"
objdump --wide --syms Kernel.obj > Kernel.map

echo "::: Making CD image"
mkdir ISO
cp ~/Cosmos/UserKit/Build/ISO/* ./ISO
cp ./Kernel.bin ./ISO/Cosmos.bin
mkisofs -relaxed-filenames -J -R -o Kernel.iso -b isolinux.bin -no-emul-boot -boot-load-size 4 -boot-info-table ./ISO

echo "::: Starting in emulator"
cp ~/Cosmos/UserKit/Build/VMware/Workstation/Filesystem.vmdk ./
qemu-system-i386 -m 128 -cdrom Kernel.iso -hda Filesystem.vmdk -vga std -boot d -no-shutdown -no-reboot

echo "::: Done"
