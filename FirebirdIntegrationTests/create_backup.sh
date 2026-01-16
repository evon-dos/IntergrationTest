#!/bin/bash

# Script to create a sample Firebird backup file for testing
# This script helps create a .fbk backup file that can be used with the RestoreDatabaseFromBackupAsync method

echo "=== Firebird Backup Creation Guide ==="
echo ""
echo "This script demonstrates how to create a Firebird backup file for testing."
echo ""
echo "Steps:"
echo "1. Run the tests first to ensure a Firebird container is running"
echo "2. Find the container ID:"
echo "   docker ps | grep firebird"
echo ""
echo "3. Create a backup inside the container:"
echo "   docker exec -it <container_id> gbak -b /firebird/data/test.fdb /firebird/data/backup.fbk -user SYSDBA -password masterkey"
echo ""
echo "4. Copy the backup file from container to your local machine:"
echo "   docker cp <container_id>:/firebird/data/backup.fbk ./TestData/sample_backup.fbk"
echo ""
echo "5. The backup file can now be used in tests with:"
echo "   await _fixture.RestoreDatabaseFromBackupAsync(\"TestData/sample_backup.fbk\");"
echo ""
echo "=== Alternative: Automated Backup Creation ==="
echo "Run this script with 'create' argument after tests are running:"
echo "./create_backup.sh create"
echo ""

if [ "$1" == "create" ]; then
    CONTAINER_ID=$(docker ps | grep firebird | awk '{print $1}' | head -n 1)
    
    if [ -z "$CONTAINER_ID" ]; then
        echo "Error: No Firebird container found. Please run the tests first."
        exit 1
    fi
    
    echo "Found Firebird container: $CONTAINER_ID"
    echo "Creating backup..."
    
    docker exec -it $CONTAINER_ID gbak -b /firebird/data/test.fdb /firebird/data/backup.fbk -user SYSDBA -password masterkey
    
    if [ $? -eq 0 ]; then
        echo "Backup created successfully inside container"
        
        mkdir -p TestData
        docker cp $CONTAINER_ID:/firebird/data/backup.fbk ./TestData/sample_backup.fbk
        
        if [ $? -eq 0 ]; then
            echo "Backup file copied to ./TestData/sample_backup.fbk"
            echo "You can now use this file in your tests!"
        else
            echo "Error: Failed to copy backup file from container"
            exit 1
        fi
    else
        echo "Error: Failed to create backup"
        exit 1
    fi
fi
