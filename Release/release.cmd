del suibot-v2-release.zip
for /r %%x in (*.exe) do 7za a -tzip "suibot-v2-release.zip" %%x
for /r %%x in (*.dll) do 7za a -tzip "suibot-v2-release.zip" %%x
for /r %%x in (*.pdb) do 7za a -tzip "suibot-v2-release.zip" %%x
7za d "suibot-v2-release.zip" "7za.exe"
