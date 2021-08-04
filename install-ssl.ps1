# Taken from
# https://github.com/rmbolger/Posh-ACME/blob/main/Tutorial.md
# https://github.com/rmbolger/Posh-ACME/blob/main/Posh-ACME/Plugins/Azure-Readme.md

if (0 -eq $args.count) {
    Write-Output "Arguments: AzureSubscriptionId resourceGroupName (domainName *.mydomain.com) contactEmail"
    exit
}
$azSubscriptionId = $args[0] # 'Azure Subscription id'
$resourceGroupName = $args[1] # 'Azure resource group name'
$domainName = $args[2] # '*.mydomain.com'
$contactEmail = $args[3] # 'asdf@mailinator.com'

# Log into Azure - this will pop up a web-GUI
Install-Module -Name Az.Accounts -Scope CurrentUser 
Import-Module Az.Accounts
$az = Connect-AzAccount

Set-AzContext -Subscription $azSubscriptionId

# Create Service Principle
Install-Module -Name Az.Resources -Scope CurrentUser 
Import-Module Az.Resources

$notBefore = Get-Date
$notAfter = $notBefore.AddYears(10)

$sp = New-AzADServicePrincipal -DisplayName PoshACME -StartDate $notBefore -EndDate $notAfter -SkipAssignment

# Assign DNS contributer role to the Service Principle
New-AzRoleAssignment -ApplicationId $sp.ApplicationId -ResourceGroupName $resourceGroupName -RoleDefinitionName 'DNS Zone Contributor'

# Install and import Posh-ACME for current user
# You can install for all users, but that requires elevated permissions
Install-Module -Name Posh-ACME -Scope CurrentUser 
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
Import-Module Posh-ACME

# Pick a certificate server.
Set-PAServer LE_PROD # Use LE_STAGE when testing

# Create a new certificate and install it
$appCred = [pscredential]::new($sp.ApplicationId,$sp.Secret)
$tenantID = $az.Context.Subscription.TenantId

$pArgs = @{
  AZSubscriptionId = $azSubscriptionId
  AZTenantId = $tenantID
  AZAppCred = $appCred
}

New-PACertificate $domainName -AcceptTOS -Contact $contactEmail -Plugin Azure -PluginArgs $pArgs -Install # -Verbose 

# Set up certificate renewal task
$taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-command "if ($cert = Submit-Renewal) { Install-PACertificate $cert }"'
$taskDescription = "Try to renew the SSL certificate from Let's Encrypt using Posh-ACME"

# Try to renew twice a day - PoshAcme won't actually request from Let's Encrypt unless due for renewal
# Don't use on-the-hour to avoid peak Let's Encrypt traffic times
$taskTriggerAM = New-ScheduledTaskTrigger -Daily -At 3:42AM  
$taskTriggerPM = New-ScheduledTaskTrigger -Daily -At 3:42PM
$taskNameAM = "Renew SSL AM"
$taskNamePM = "Renew SSL PM"

# Make sure renewal task is run as current user, whether logged in or not
# Current user must be used as renewal details are encrypted under their account
$user = "$env:UserDomain\$env:UserName"
$credentials = Get-Credential -Credential $user
$password = $credentials.GetNetworkCredential().Password

# Register renewal with Windows Task Scheduler
Register-ScheduledTask -TaskName $taskNameAM -Action $taskAction -Trigger $taskTriggerAM -Description $taskDescription -User $user -Password $password
Register-ScheduledTask -TaskName $taskNamePM -Action $taskAction -Trigger $taskTriggerPM -Description $taskDescription -User $user -Password $password
