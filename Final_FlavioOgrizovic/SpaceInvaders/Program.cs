using Raylib_cs;
using System.Numerics;

class Program
{
    // VENTANA //

    static int screenWidth = 1240;
    static int screenHeight = 720;

    // PANTALLAS //

    enum GameState { Start, Play, GameOver, Win }
    static GameState currentState = GameState.Start;

    // JUGADOR //

    static int playerLives = 3;
    static int score = 0;
    static int highscore = 0;

    static Vector2 playerPosition;
    static List<Vector2> playerShots = new List<Vector2>();

    static Texture2D playerTexture;
    static Texture2D playerBulletTexture;

    // ENEMIGOS //

    static List<(Vector2 position, Color color)> enemyShots = new List<(Vector2 position, Color color)>();
    static List<(Vector2 position, Texture2D texture, Color color)> enemies = new List<(Vector2 position, Texture2D texture, Color color)>();

    static bool enemiesMovingRight = true;

    static Texture2D alienTexture1;
    static Texture2D alienTexture2;
    static Texture2D alienTexture3;
    static Texture2D enemyBulletTexture;

    // RANDOM //

    static Random random = new Random();

    public static void Main()
    {
        // SETEO DE VENTANA Y FPS //

        Raylib.InitWindow(screenWidth, screenHeight, "Space Invaders");
        Raylib.SetTargetFPS(60);

        // CARGA DE TEXTURAS //

        playerTexture = Raylib.LoadTexture("resources/player.png");
        playerBulletTexture = Raylib.LoadTexture("resources/bullet.png");
        alienTexture1 = Raylib.LoadTexture("resources/alien1.png");
        alienTexture2 = Raylib.LoadTexture("resources/alien2.png");
        alienTexture3 = Raylib.LoadTexture("resources/alien3.png");
        enemyBulletTexture = Raylib.LoadTexture("resources/bullet.png");

        // CARGA DE HIGHSCORE Y RESETEO DE JUEGO //

        LoadHighscore();
        ResetGame();

        while (!Raylib.WindowShouldClose())
        {
            // SWITCH PARA MANEJAR LAS DISTINTAS PANTALLAS //

            switch (currentState)
            {
                case GameState.Start:
                    DrawStartScreen();
                    break;
                case GameState.Play:
                    PlayGame();
                    break;
                case GameState.GameOver:
                    DrawGameOverScreen();
                    break;
                case GameState.Win:
                    DrawWinScreen();
                    break;
            }
        }

        // DESCARGA DE TEXTURAS //

        Raylib.UnloadTexture(playerTexture);
        Raylib.UnloadTexture(playerBulletTexture);
        Raylib.UnloadTexture(alienTexture1);
        Raylib.UnloadTexture(alienTexture2);
        Raylib.UnloadTexture(alienTexture3);
        Raylib.UnloadTexture(enemyBulletTexture);

        Raylib.CloseWindow();
    }

    private static void DrawStartScreen()
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            currentState = GameState.Play;
        }

        Raylib.BeginDrawing();

        Raylib.ClearBackground(Color.Black);

        // TEXTO DE PANTALLA INICIAL //

        Raylib.DrawText("SPACE INVADERS", screenWidth / 2 - 250, screenHeight / 4, 50, Color.Violet);
        Raylib.DrawText("Developed by Flavio Ogrizovic", screenWidth / 2 - 200, screenHeight / 4 + 60, 25, Color.White);
        Raylib.DrawText("Click to Start", screenWidth / 2 - 130, screenHeight / 2, 30, Color.White);
        Raylib.DrawText($"Highscore: {highscore} points", screenWidth / 2 - 130, screenHeight / 2 + 100, 20, Color.Gold);

        Raylib.EndDrawing();
    }

    private static void PlayGame()
    {
        // MOVIENTO DEL JUGADOR EN X //

        if (Raylib.IsKeyDown(KeyboardKey.Left))
        {
            playerPosition.X -= 5f;
        }
        if (Raylib.IsKeyDown(KeyboardKey.Right))
        {
            playerPosition.X += 5f;
        }

        // LIMITO EL MOVIMIENTO DEL JUGADOR EN X //

        playerPosition.X = Math.Clamp(playerPosition.X, 0, screenWidth - playerTexture.Width);

        // DISPARO DEL JUGADOR //

        playerShoot();

        // DISPARO DE LOS ENEMIGOS //

        EnemyShoot();

        // COLISIONES //

        CheckPlayerShotsCollisions();
        CheckEnemyShotsCollisions();

        // MOVIMIENTO DE ENEMIGOS //

        MoveEnemies();

        Raylib.BeginDrawing();

        Raylib.ClearBackground(Color.Black);

        Raylib.DrawText($"LIVES: {playerLives}", 10, 10, 30, Color.White);
        Raylib.DrawText($"SCORE: {score}", 10, 40, 30, Color.Gold);

        DrawPlayer();
        DrawPlayerShots();
        DrawEnemies();
        DrawEnemyShots();

        Raylib.EndDrawing();
    }

    private static void playerShoot()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
           playerShots.Add(new Vector2(playerPosition.X + playerTexture.Width / 2 - playerBulletTexture.Width / 2, playerPosition.Y));
        }

        for (int i = playerShots.Count - 1; i >= 0; i--)
        {
            playerShots[i] -= new Vector2(0, 5);

            // Eliminar balas fuera de la pantalla

            if (playerShots[i].Y < 0)
            {
                playerShots.RemoveAt(i);
            }
        }
    }

    private static void EnemyShoot()
    {
        const float shootProbability = 0.02f; // 2%

        if (random.NextDouble() < shootProbability && enemies.Count > 0)
        {
            int randomEnemyIndex = random.Next(0, enemies.Count); // Selecciono un enemigo aleatorio
            var randomEnemy = enemies[randomEnemyIndex];

            enemyShots.Add((new Vector2(
                randomEnemy.position.X + alienTexture1.Width / 2, // Posición en x debajo del enemigo en el medio del enemigo
                randomEnemy.position.Y + alienTexture1.Height // Posición en y debajodel enemigo
            ), randomEnemy.color)); // Mismo color que el enemigo
        }

        // Mover las balas hacia abajo

        for (int i = enemyShots.Count - 1; i >= 0; i--)
        {
            enemyShots[i] = (enemyShots[i].position + new Vector2(0, 5), enemyShots[i].color);

            // Eliminar balas fuera de la pantalla

            if (enemyShots[i].position.Y > screenHeight)
            {
                enemyShots.RemoveAt(i);
            }
        }
    }

    private static void CheckPlayerShotsCollisions()
    {
        // Recorro las listas de atrás hacia adelante para evitar problemas con los índices cuando elimino un elemento

        for (int i = playerShots.Count - 1; i >= 0; i--) // Recorro los disparos del jugador
        {
            for (int j = enemies.Count - 1; j >= 0; j--) // Recorro los enemigos
            {
                // Chequeo de colisiones

                if (Raylib.CheckCollisionRecs(
                    new Rectangle(playerShots[i].X, playerShots[i].Y, playerBulletTexture.Width, playerBulletTexture.Height),
                    new Rectangle(enemies[j].position.X, enemies[j].position.Y, alienTexture1.Width, alienTexture1.Height)))
                {
                    // Elimino el disparo y el enemigo si colisionan; sumo puntaje

                    playerShots.RemoveAt(i);
                    enemies.RemoveAt(j);
                    score += 10;

                    // Win condition

                    if (enemies.Count == 0)
                    {
                        currentState = GameState.Win;
                    }

                    break; // Salgo del bucle de enemigos si el disparo impactó
                }
            }
        }
    }

    private static void CheckEnemyShotsCollisions()
    {
        // Misma lógica pero solo recorro la lista de disparos de los enemigos y los comparo con el rectángulo del jugador

        for (int i = enemyShots.Count - 1; i >= 0; i--)
        {
            if (Raylib.CheckCollisionRecs(
                new Rectangle(enemyShots[i].position.X, enemyShots[i].position.Y, enemyBulletTexture.Width, enemyBulletTexture.Height),
                new Rectangle(playerPosition.X, playerPosition.Y, playerTexture.Width, playerTexture.Height)))
            {
                // Elimino la bala y el jugador pierde una vida

                enemyShots.RemoveAt(i);
                playerLives--;

                // Lose condition

                if (playerLives <= 0)
                {
                    currentState = GameState.GameOver;
                }

                continue; // Sigo chequeando colisiones
            }
        }
    }

    private static void DrawPlayerShots()
    {
        foreach (var shot in playerShots)
        {
            Raylib.DrawTexture(playerBulletTexture, (int)shot.X, (int)shot.Y, Color.Purple);
        }
    }

    private static void DrawPlayer()
    {
        Raylib.DrawTexture(playerTexture, (int)playerPosition.X, (int)playerPosition.Y, Color.Purple);
    }

    private static void DrawEnemies()
    {
        foreach (var enemy in enemies)
        {
            Raylib.DrawTexture(enemy.texture, (int)enemy.position.X, (int)enemy.position.Y, enemy.color);
        }
    }

    private static void DrawEnemyShots()
    {
        foreach (var enemyShot in enemyShots)
        {
            Raylib.DrawTexture(enemyBulletTexture, (int)enemyShot.position.X, (int)enemyShot.position.Y, enemyShot.color);
        }
    }

    private static void DrawGameOverScreen()
    {
        SaveHighscore();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            currentState = GameState.Play;
            ResetGame();
        }

        Raylib.BeginDrawing();

        Raylib.ClearBackground(Color.Black);

        Raylib.DrawText($"GAME OVER", screenWidth / 2 - 175, screenHeight / 4, 50, Color.Red);
        Raylib.DrawText($"You scored {score} points", screenWidth / 2 - 130, screenHeight / 4 + 120, 20, Color.White);
        Raylib.DrawText($"Highscore: {highscore} points", screenWidth / 2 - 130, screenHeight / 4 + 160, 20, Color.Gold);
        Raylib.DrawText("Click to play again", screenWidth / 2 - 160, screenHeight / 4 + 250, 30, Color.White);

        Raylib.EndDrawing();
    }

    static void DrawWinScreen()
    {
        SaveHighscore();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            currentState = GameState.Play;
            ResetGame();
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.DrawText("¡YOU WIN!", screenWidth / 2 - 150, screenHeight / 4, 50, Color.Green);
        Raylib.DrawText($"Final Score: {score} points", screenWidth / 2 - 145, screenHeight / 4 + 120, 20, Color.White);
        Raylib.DrawText($"Highscore: {highscore} points", screenWidth / 2 - 135, screenHeight / 4 + 160, 20, Color.Gold);
        Raylib.DrawText("Click to play again", screenWidth / 2 - 170, screenHeight / 4 + 250, 30, Color.White);

        Raylib.EndDrawing();
    }

    private static void InitializeEnemies()
    {
        // Limpio la lista antes de agregar nuevos enemigos por si reseteo el juego

        enemies.Clear();

        // Texturas y colores

        Texture2D[] textures = { alienTexture1, alienTexture2, alienTexture3 };
        Color[] colors = { Color.Blue, Color.Green, Color.Red };

        for (int i = 0; i < 6; i++) // Columnas
        {
            for (int j = 0; j < 3; j++) // Filas
            {
                Vector2 position = new Vector2(100 + i * 100, 100 + j * 100); // La posición inicial en X de los enemigos es 100 + 100 por su índice. Lo mismo para la posición en Y
                Texture2D texture = textures[j % textures.Length]; // Una textura por cada fila (0, 1, 2). Me aseguro que el índice siempre está dentro del rango de los arrays.
                Color color = colors[j % colors.Length];
                enemies.Add((position, texture, color));
            }
        }
    }

    private static void MoveEnemies()
    {
        float speed = 2;
        float direction = enemiesMovingRight ? 1 : -1; // Direction vale 1 si los enemigos van hacia la derecha; es -1 si van a la izquierda

        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i] = (enemies[i].position + new Vector2(direction * speed, 0), enemies[i].texture, enemies[i].color);
        }

        if (enemies.Exists(enemy => enemy.position.X > screenWidth - alienTexture1.Width)) //Si al menos un enemigo está más allá del borde derecho de la pantalla...
        {
            enemiesMovingRight = false; // Cambia la dirección hacia la izquierda
        }
        if (enemies.Exists(enemy => enemy.position.X < 0)) // Si algún enemigo toca el borde izquierdo...
        {
            enemiesMovingRight = true; // Cambia la dirección
        }
    }

    private static void ResetGame()
    {
        // Reseteo de variables 

        score = 0;
        playerLives = 3;
        playerPosition = new Vector2(screenWidth / 2, screenHeight - playerTexture.Height - 20);

        // Limpio las listas de disparos

        playerShots.Clear();
        enemyShots.Clear();

        InitializeEnemies();
    }

    private static void SaveHighscore()
    {
        if (highscore < score)
        {
            highscore = score;
            File.WriteAllText("Highscore.txt", highscore.ToString());
        }
    }

    private static void LoadHighscore()
    {
        if (File.Exists("Highscore.txt"))
        {
            highscore = int.Parse(File.ReadAllText("Highscore.txt"));
        }
    }
}
