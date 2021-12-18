###########################################
#-------------) System Info (-------------#
###########################################

#-- SY) OS
print_2title "Operative system"
print_info "https://book.hacktricks.xyz/linux-unix/privilege-escalation#kernel-exploits"
(cat /proc/version || uname -a ) 2>/dev/null | sed -${E} "s,$kernelDCW_Ubuntu_Precise_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_5,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_6,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Xenial,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel7,${SED_RED_YELLOW}," | sed -${E} "s,$kernelB,${SED_RED},"
warn_exec lsb_release -a 2>/dev/null
if [ "$MACPEAS" ]; then
warn_exec system_profiler SPSoftwareDataType
fi
echo ""

#-- SY) Sudo
print_2title "Sudo version"
if [ "$(command -v sudo 2>/dev/null)" ]; then
print_info "https://book.hacktricks.xyz/linux-unix/privilege-escalation#sudo-version"
sudo -V 2>/dev/null | grep "Sudo ver" | sed -${E} "s,$sudovB,${SED_RED},"
else echo_not_found "sudo"
fi
echo ""

#--SY) USBCreator
print_2title "USBCreator"
print_info "https://book.hacktricks.xyz/linux-unix/privilege-escalation/d-bus-enumeration-and-command-injection-privilege-escalation"
if busctl list 2>/dev/null | grep -q com.ubuntu.USBCreator; then
pc_version=$(dpkg -l 2>/dev/null | grep policykit-desktop-privileges | grep -oP "[0-9][0-9a-zA-Z\.]+")
if [ -z "$pc_version" ]; then
    pc_version=$(apt-cache policy policykit-desktop-privileges 2>/dev/null | grep -oP "\*\*\*.*" | cut -d" " -f2)
fi
if [ -n "$pc_version" ]; then
    pc_length=${#pc_version}
    pc_major=$(echo "$pc_version" | cut -d. -f1)
    pc_minor=$(echo "$pc_version" | cut -d. -f2)
    if [ "$pc_length" -eq 4 ] && [ "$pc_major" -eq 0 ] && [ "$pc_minor"  -lt 21 ]; then
        echo "Vulnerable!!" | sed -${E} "s,.*,${SED_RED},"
    fi
fi
fi
echo ""

#-- SY) PATH
print_2title "PATH"
print_info "https://book.hacktricks.xyz/linux-unix/privilege-escalation#writable-path-abuses"
echo "$OLDPATH" 2>/dev/null | sed -${E} "s,$Wfolders|\./|\.:|:\.,${SED_RED_YELLOW},g"
echo "New path exported: $PATH" 2>/dev/null | sed -${E} "s,$Wfolders|\./|\.:|:\. ,${SED_RED_YELLOW},g"
echo ""

#-- SY) Date
print_2title "Date & uptime"
warn_exec date 2>/dev/null
warn_exec uptime 2>/dev/null
echo ""

#-- SY) System stats
print_2title "System stats"
(df -h || lsblk) 2>/dev/null || echo_not_found "df and lsblk"
warn_exec free 2>/dev/null
echo ""

#-- SY) CPU info
print_2title "CPU info"
warn_exec lscpu 2>/dev/null
echo ""

#-- SY) Environment vars
print_2title "Environment"
print_info "Any private information inside environment variables?"
(env || printenv || set) 2>/dev/null | grep -v "RELEVANT*|FIND*|^VERSION=|dbuslistG|mygroups|ldsoconfdG|pwd_inside_history|kernelDCW_Ubuntu_Precise|kernelDCW_Ubuntu_Trusty|kernelDCW_Ubuntu_Xenial|kernelDCW_Rhel|^sudovB=|^rootcommon=|^mounted=|^mountG=|^notmounted=|^mountpermsB=|^mountpermsG=|^kernelB=|^C=|^RED=|^GREEN=|^Y=|^B=|^NC=|TIMEOUT=|groupsB=|groupsVB=|knw_grps=|sidG|sidB=|sidVB=|sidVB2=|sudoB=|sudoG=|sudoVB=|timersG=|capsB=|notExtensions=|Wfolders=|writeB=|writeVB=|_usrs=|compiler=|PWD=|LS_COLORS=|pathshG=|notBackup=|processesDump|processesB|commonrootdirs" | sed -${E} "s,[pP][wW][dD]|[pP][aA][sS][sS][wW]|[aA][pP][iI][kK][eE][yY]|[aA][pP][iI][_][kK][eE][yY]|KRB5CCNAME,${SED_RED},g" || echo_not_found "env || set"
echo ""

#-- SY) Dmesg
print_2title "Searching Signature verification failed in dmseg"
print_info "https://book.hacktricks.xyz/linux-unix/privilege-escalation#dmesg-signature-verification-failed"
(dmesg 2>/dev/null | grep "signature") || echo_not_found "dmesg"
echo ""

#-- SY) Kernel extensions
if [ "$MACPEAS" ]; then
    print_2title "Kernel Extensions not belonging to apple"
    kextstat 2>/dev/null | grep -Ev " com.apple."

    print_2title "Unsigned Kernel Extensions"
    macosNotSigned /Library/Extensions
    macosNotSigned /System/Library/Extensions
fi

if [ "$(command -v bash 2>/dev/null)" ]; then
    print_2title "Executing Linux Exploit Suggester"
    print_info "https://github.com/mzet-/linux-exploit-suggester"
    les_b64="peass{LES}"
    echo $les_b64 | base64 -d | bash
    echo ""
fi

if [ "$(command -v perl 2>/dev/null)" ]; then
    print_2title "Executing Linux Exploit Suggester 2"
    print_info "https://github.com/jondonas/linux-exploit-suggester-2"
    les2_b64="peass{LES2}"
    echo $les2_b64 | base64 -d | perl
    echo ""
fi

if [ "$(command -v brew 2>/dev/null)" ]; then
    print_2title "Brew Doctor Suggestions"
    brew doctor
    echo ""
fi



#-- SY) AppArmor
print_2title "Protections"
print_list "AppArmor enabled? .............. "$NC
if [ "$(command -v aa-status 2>/dev/null)" ]; then
    aa-status 2>&1 | sed "s,disabled,${SED_RED},"
elif [ "$(command -v apparmor_status 2>/dev/null)" ]; then
    apparmor_status 2>&1 | sed "s,disabled,${SED_RED},"
elif [ "$(ls -d /etc/apparmor* 2>/dev/null)" ]; then
    ls -d /etc/apparmor*
else
    echo_not_found "AppArmor"
fi

#-- SY) grsecurity
print_list "grsecurity present? ............ "$NC
( (uname -r | grep "\-grsec" >/dev/null 2>&1 || grep "grsecurity" /etc/sysctl.conf >/dev/null 2>&1) && echo "Yes" || echo_not_found "grsecurity")

#-- SY) PaX
print_list "PaX bins present? .............. "$NC
(command -v paxctl-ng paxctl >/dev/null 2>&1 && echo "Yes" || echo_not_found "PaX")

#-- SY) Execshield
print_list "Execshield enabled? ............ "$NC
(grep "exec-shield" /etc/sysctl.conf 2>/dev/null || echo_not_found "Execshield") | sed "s,=0,${SED_RED},"

#-- SY) SElinux
print_list "SELinux enabled? ............... "$NC
(sestatus 2>/dev/null || echo_not_found "sestatus") | sed "s,disabled,${SED_RED},"

#-- SY) Gatekeeper
if [ "$MACPEAS" ]; then
    print_list "Gatekeeper enabled? .......... "$NC
    (spctl --status 2>/dev/null || echo_not_found "sestatus") | sed "s,disabled,${SED_RED},"

    print_list "sleepimage encrypted? ........ "$NC
    (sysctl vm.swapusage | grep "encrypted" | sed "s,encrypted,${SED_GREEN},") || echo_no

    print_list "XProtect? .................... "$NC
    (system_profiler SPInstallHistoryDataType 2>/dev/null | grep -A 4 "XProtectPlistConfigData" | tail -n 5 | grep -Iv "^$") || echo_no

    print_list "SIP enabled? ................. "$NC
    csrutil status | sed "s,enabled,${SED_GREEN}," | sed "s,disabled,${SED_RED}," || echo_no

    print_list "Connected to JAMF? ........... "$NC
    warn_exec jamf checkJSSConnection

    print_list "Connected to AD? ............. "$NC
    dsconfigad -show && echo "" || echo_no
fi

#-- SY) ASLR
print_list "Is ASLR enabled? ............... "$NC
ASLR=$(cat /proc/sys/kernel/randomize_va_space 2>/dev/null)
if [ -z "$ASLR" ]; then
    echo_not_found "/proc/sys/kernel/randomize_va_space";
else
    if [ "$ASLR" -eq "0" ]; then printf $RED"No"$NC; else printf $GREEN"Yes"$NC; fi
    echo ""
fi

#-- SY) Printer
print_list "Printer? ....................... "$NC
(lpstat -a || system_profiler SPPrintersDataType || echo_no) 2>/dev/null

#-- SY) Running in a virtual environment
print_list "Is this a virtual machine? ..... "$NC
hypervisorflag=$(grep flags /proc/cpuinfo 2>/dev/null | grep hypervisor)
if [ "$(command -v systemd-detect-virt 2>/dev/null)" ]; then
    detectedvirt=$(systemd-detect-virt)
    if [ "$hypervisorflag" ]; then printf $RED"Yes ($detectedvirt)"$NC; else printf $GREEN"No"$NC; fi
else
    if [ "$hypervisorflag" ]; then printf $RED"Yes"$NC; else printf $GREEN"No"$NC; fi
fi
