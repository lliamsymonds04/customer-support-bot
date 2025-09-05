#!/bin/bash
# deploy-eb.sh
# Usage: ./deploy-eb.sh

# Exit on errors
set -e

# Configuration
APP_NAME="backend"          # Name of your DLL/project (without .dll)
OUTPUT_DIR="publish"        # Folder for published files
ZIP_FILE="app.zip"          # Final zip to upload to EB
RUNTIME="linux-x64"         # Target runtime
PORT=5000                   # Port EB will proxy to

echo "Cleaning previous publish..."
rm -rf $OUTPUT_DIR
rm -f $ZIP_FILE

echo "Publishing .NET app for Linux..."
dotnet publish -c Release -r $RUNTIME --self-contained false -o $OUTPUT_DIR

echo "Creating Procfile..."
# Procfile content with 0.0.0.0 binding
echo "web: dotnet ${APP_NAME}.dll --urls http://0.0.0.0:${PORT}" > $OUTPUT_DIR/Procfile

echo "Zipping contents for Elastic Beanstalk..."
cd $OUTPUT_DIR
zip -r ../$ZIP_FILE .
cd ..

echo "Deployment zip ready: $ZIP_FILE"