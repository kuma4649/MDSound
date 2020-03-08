echo mml2vgm

mkdir output
del /Q .\output\*.*
xcopy .\MDSound\MDSound\bin\Release\netstandard2.0\*.* .\output /E /R /Y /I /K

copy /Y .\MDSound\CHANGE.txt .\output
copy /Y .\LICENSE.txt .\output
copy /Y .\PSG2.txt .\output
copy /Y .\README.md .\output
copy /Y .\YM2609.txt .\output

del /Q .\output\*.pdb
del /Q .\output\*.json
del /Q .\output\*.config
del /Q .\output\*.wav
del /Q .\output\*.config
del /Q .\output\bin.zip

pause
