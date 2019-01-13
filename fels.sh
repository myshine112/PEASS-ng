#!/bin/sh

file="/tmp/fels"
RED='\033[0;31m'
Y='\033[0;33m'
NC='\033[0m'

rm -rf $file

printf $Y"[+] "$RED"System info\n"$NC >> $file
uname -a 2>/dev/null >> $file

printf $Y"[+] "$RED"nc, wget, curl, ping, gcc, gdb, base64?\n"$NC >> $file
which nc wget curl ping gcc make gdb base64 2>/dev/null >> $file

printf $Y"[+] "$RED"Hostname, hosts and DNS\n"$NC >> $file
cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null >> $file

printf $Y"[+] "$RED"My username and groups\n"$NC >> $file
whoami 2>/dev/null >> $file
groups 2>/dev/null >> $file

printf $Y"[+] "$RED"Files inside HOME (limit 20)\n"$NC >> $file
ls -la $HOME 2>/dev/null | head -n 20 >> $file

printf $Y"[+] "$RED"Do I hace PGP keys?\n"$NC >> $file
gpg --list-keys 2>/dev/null >> $file

printf $Y"[+] "$RED"Users with console\n"$NC >> $file
cat /etc/passwd | grep "sh$" >> $file

printf $Y"[+] "$RED"20 First files of /home\n"$NC >> $file
find /home -type f -printf "%f\t%p\t%u\t%g\t%m\n" 2>/dev/null | column -t | head -n 20 >> $file

printf $Y"[+] "$RED"Any file inside accesible .ssh directory?\n"$NC >> $file
find  /home -name .ssh 2>/dev/null -exec ls -laR {} \; >> $file

printf $Y"[+] "$RED"Environment\n"$NC >> $file
env 2>/dev/null >> $file

printf $Y"[+] "$RED"Cleaned proccesses\n"$NC >> $file
ps -ef 2>/dev/null | grep -v "\[" >> $file

printf $Y"[+] "$RED"Networks\n"$NC >> $file
ifconfig 2>/dev/null >> $file

printf $Y"[+] "$RED"Ports\n"$NC >> $file
netstat -punta 2>/dev/null >> $file

printf $Y"[+] "$RED"SUID files\n"$NC >> $file
find / -perm -4000 2>/dev/null >> $file

printf $Y"[+] "$RED"GUID files\n"$NC >> $file
find / -perm -g=s -type f 2>/dev/null >> $file

printf $Y"[+] "$RED"Capabilities\n"$NC >> $file
getcap -r / 2>/dev/null >> $file

printf $Y"[+] "$RED"Can I sniff with tcpdump?\n"$NC >> $file
timeout 1 tcpdump >> $file 2>&1

printf $Y"[+] "$RED"Find IPs inside logs\n"$NC >> $file
grep -a -R -o '[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}' /var/log/ 2>/dev/null | sort | uniq >> $file

printf $Y"[+] "$RED"Find password string inside /home, /var/www, /var/log\n"$NC >> $file
grep -lri "password" /home /var/www /var/log 2>/dev/null >> $file

printf $Y"[+] "$RED"Sudo -l (you need to puts the password and the result appear in console)\n"$NC >> $file
sudo -l 
