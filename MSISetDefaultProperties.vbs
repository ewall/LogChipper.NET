set objInstaller = CreateObject("WindowsInstaller.Installer")
set objDatabase = objInstaller.OpenDatabase("LogChipperSetup\Release\LogChipperSetup.msi", 1)

strWQL = "SELECT Property, Value FROM Property Where Property = 'EDITA1'"
Set objMSIView = objDatabase.OpenView(strWQL)
objMSIView.Execute
Set objMSIRecord = objMSIView.Fetch
objMSIRecord.StringData(2) = "C:\Temp\test.log"
objMSIView.Modify 2, objMSIRecord
objDatabase.Commit

strWQL = "SELECT Property, Value FROM Property Where Property = 'EDITA2'"
Set objMSIView = objDatabase.OpenView(strWQL)
objMSIView.Execute
Set objMSIRecord = objMSIView.Fetch
objMSIRecord.StringData(2) = "localhost"
objMSIView.Modify 2, objMSIRecord
objDatabase.Commit

strWQL = "SELECT Property, Value FROM Property Where Property = 'EDITA3'"
Set objMSIView = objDatabase.OpenView(strWQL)
objMSIView.Execute
Set objMSIRecord = objMSIView.Fetch
objMSIRecord.StringData(2) = "514"
objMSIView.Modify 2, objMSIRecord
objDatabase.Commit

strWQL = "SELECT Property, Value FROM Property Where Property = 'EDITA4'"
Set objMSIView = objDatabase.OpenView(strWQL)
objMSIView.Execute
Set objMSIRecord = objMSIView.Fetch
objMSIRecord.StringData(2) = "TCP"
objMSIView.Modify 2, objMSIRecord
objDatabase.Commit
