dotnet publish -r linux-arm
pushd .\bin\Debug\netcoreapp3.0\linux-arm\publish
scp .\* pi@joespi:/home/pi/SmartHomePi
popd