# WorkServiceKillOnTime
Proyecto de C# que funciona como servicio cierra procesos a la hora y nombre espeficicados

## Instalacion

Usando CMD como administrador, el espacio despues de binpath es importante

sc.exe create "Kill on Time Service" binpath= "RUTA" start=auto

## Eliminar

sc.exe delete "Kill on Time Service"

## Configurar
Para configurar modificar appsettings.json con la hora en que quieres que se cierren los procesos y los nombres de los procesos. El nombre del Exe es sin .exe y es Case Sensitive