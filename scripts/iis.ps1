$app = $args[0]
$site = $args[1]
$path = $args[2]
$pool = $args[3]
New-WebApplication -Site $site $app -PhysicalPath $path -ApplicationPool $pool
Set-WebConfiguration system.webServer/security/authentication/anonymousAuthentication -PSPath IIS:\ -Location $site/$app -Value @{enabled="False"}
Set-WebConfiguration system.webServer/security/authentication/basicAuthentication -PSPath IIS:\ -Location $site/$app -Value @{enabled="True"}
Set-WebConfiguration system.webServer/security/authentication/basicAuthentication -PSPath IIS:\ -Location $site/$app -Value @{defaultLogonDomain="adc.analit.net"}
Set-WebConfiguration system.webServer/security/authentication/basicAuthentication -PSPath IIS:\ -Location $site/$app -Value @{realm="adc.analit.net"}
