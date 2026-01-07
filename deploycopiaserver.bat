# Ferma IIS
iisreset /stop

# Copia i file dal tuo PC
Copy-Item -Path "\\IT15\C$\STUDIOCG\StudioCG\publish\*" -Destination "C:\inetpub\studiocg" -Recurse -Force

# Riavvia IIS
iisreset /start