# All files in this directory will be copied to the root of the container

echo "Building..."

echo "Building linux-x64 not self-contained"
dotnet publish -r linux-x64 -p:PublishSingleFile=false --self-contained true -c Release -o ./publish/linux-x64

echo "Building win-x64 not self-contained"
dotnet publish -r win-x64 -p:PublishSingleFile=false --self-contained true -c Release -o ./publish/win-x64

echo "Building osx-x64 not self-contained"
dotnet publish -r osx-x64 -p:PublishSingleFile=false --self-contained true -c Release -o ./publish/osx-x64

#One file per platform
echo "Building linux-x64 self-contained"
dotnet publish -r linux-x64 -p:PublishSingleFile=true --self-contained true -c Release -o ./publish/linux-x64-selfcontained

echo "Building win-x64 self-contained"
dotnet publish -r win-x64 -p:PublishSingleFile=true --self-contained true -c Release -o ./publish/win-x64-selfcontained

echo "Building osx-x64 self-contained"
dotnet publish -r osx-x64 -p:PublishSingleFile=true --self-contained true -c Release -o ./publish/osx-x64-selfcontained

echo "Zipping..."
mkdir ./publish/zip


zip -r ./publish/zip/linux-x64.zip ./publish/linux-x64
zip -r ./publish/zip/win-x64.zip ./publish/win-x64
zip -r ./publish/zip/osx-x64.zip ./publish/osx-x64

zip -r ./publish/zip/linux-x64-selfcontained.zip ./publish/linux-x64-selfcontained
zip -r ./publish/zip/win-x64-selfcontained.zip ./publish/win-x64-selfcontained
zip -r ./publish/zip/osx-x64-selfcontained.zip ./publish/osx-x64-selfcontained

echo "Done!"