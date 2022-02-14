dotnet publish -r win-x64 -c Release  /p:PublishDir=bin\Release\publish\ /p:PublishSingleFile=true /p:PublishTrimmed=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true

IF EXIST bin\Release\publish\ExcelExport.exe\ (xcopy /Q /E /Y /I bin\Release\publish\ExcelExport.exe run > nul) ELSE (xcopy /Q /Y /I bin\Release\publish\ExcelExport.exe run > nul)