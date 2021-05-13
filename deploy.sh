#!/bin/bash
# Example of deploying to a linux pc
ssh support@192.168.4.90 'cd /home/support/Documents && rm GcodeController'
dotnet publish -r linux-x64 -p:PublishSingleFile=true
scp GcodeController/bin/Debug/net5.0/linux-x64/publish/GcodeController support@192.168.4.90:/home/support/Documents
ssh support@192.168.4.90 'cd /home/support/Documents && chmod u+x GcodeController'
