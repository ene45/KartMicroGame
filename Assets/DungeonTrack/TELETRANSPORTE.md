# Regreso a la entrada de la mazmorra

En **DungeonCircuit_AnilloLava 1**, el objeto **Sala_Suelo_Libre_Central** tiene el componente **Kart Teleport Surface**. Cuando el jugador cae y su carrocería o cualquiera de sus ruedas toca ese suelo, reaparece antes del portal de entrada, mirando hacia los tres caminos.

El suelo conserva su collider sólido. Las rocas y los demás objetos no se teletransportan.

El destino es un GameObject vacío llamado **Punto_Reaparicion_Entrada**, dentro de **CIRCUITO_MAZMORRA_PROBUILDER**. Podés moverlo con Move y orientarlo con Rotate; su eje azul (Z) indica hacia dónde mirará el kart. Mantené el punto unos 0,65 m por encima del piso para no reaparecer dentro de la pista.

En el Inspector del suelo:

- **Destination**: el Transform del objeto vacío. Podés reemplazarlo por otro.
- **Player**: el ArcadeKart del jugador.
- **Cooldown**: espera entre teletransportes, inicialmente 0,75 s, para ignorar contactos duplicados.

El teletransporte quita la velocidad y el giro, limpia el derrape y los bloqueos temporales de trampas, y reposiciona la cámara de seguimiento. No reinicia las vueltas ni habilita el movimiento durante la cuenta regresiva o el fin de carrera.

Para configurar una copia del circuito: **Tools → Dungeon Track → Teletransporte → Configurar regreso desde el suelo central**. Admite Undo y conserva la posición y orientación del destino si ya existe. No vuelve a generar la pista.
