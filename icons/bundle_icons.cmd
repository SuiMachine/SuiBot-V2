set "curpath=%cd%"

C:
cd C:\icobundle-1.2b1
icobundl.exe -o "%curpath%\result.ico" "%curpath%\16.ico" "%curpath%\32.ico" "%curpath%\48.ico" "%curpath%\256.ico"
pause