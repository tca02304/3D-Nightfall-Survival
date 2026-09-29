result=`"$1" yesno-msgbox --no-cancel ---title "Intersoft Solutions" --text "Crosslight is not installed" --informative-text "Crosslight is not installed in this machine. For best development experience such as IDE integration and templates, it is highly recommended to download and install Crosslight full setup to this machine.

Would you like to do this now?

Note: If you choose No, your packages will still be installed properly and all runtime features should work properly.
" --float`
if [ $result == "1" ]
then
    open http://www.intersoftsolutions.com/RequestTrial
fi