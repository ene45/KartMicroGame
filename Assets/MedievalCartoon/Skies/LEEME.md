# Cielo nocturno cartoon

En **DungeonCircuit_AnilloLava 1**, seleccioná **CIELO_NOCTURNO_LUNA** en Hierarchy. El Inspector muestra los colores del cielo, las estrellas, las nubes, el tamaño de la luna y la iluminación ambiental.

En la sección **Directional Light de la luna**, podés ajustar:

- **Altura de la luna** y **Dirección alrededor de la pista**: mueven la luna del skybox y orientan su luz juntas.
- **Color, Intensity y Shadows**: son los controles nativos de la Directional Light, mostrados en el mismo Inspector. También podés seleccionar **Luna_Directional_Light** y modificar su rotación directamente.
- **Brillo del cielo / Halo lunar**: cambian la apariencia del fondo sin alterar la intensidad de la luz que recibe el kart.

La luz ambiental suave mantiene visible la pista. Las antorchas y la lava conservan su iluminación cálida. No se agregan scripts de ciclo día/noche ni dependencias de imágenes externas: el shader dibuja un cielo gris azulado, nubes estilizadas, estrellas y una luna facetada con cráteres discretos.

Para usarlo en otra escena: **Tools → Assets de Mazmorra → Cielo nocturno → Aplicar a la escena actual**. Este comando admite Undo, reutiliza la Directional Light de esa escena y crea un material propio en **Skies/Perfiles**. Las configuraciones de otros circuitos quedan independientes.

También hay un prefab reutilizable en **Assets/MedievalCartoon/Skies/Noche_Cartoon_Con_Luna.prefab**. Si lo colocás manualmente en varias escenas, duplicá el material para cada una. En Play Mode se usa una copia temporal del material y se restaura el original al salir.
