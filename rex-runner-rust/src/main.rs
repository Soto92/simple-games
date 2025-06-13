use crossterm::{
    cursor::{Hide, MoveTo, Show},
    event::{poll, read, Event, KeyCode, KeyEvent, KeyModifiers},
    execute,
    style::{Print, Stylize},
    terminal::{
        disable_raw_mode, enable_raw_mode, Clear, ClearType, EnterAlternateScreen,
        LeaveAlternateScreen,
    },
};
use rand::Rng;
use std::io::{stdout, Stdout, Write};
use std::time::Duration;
use std::thread;

const SCREEN_WIDTH: u16 = 60;
const SCREEN_HEIGHT: u16 = 15;
const GROUND_Y: u16 = SCREEN_HEIGHT - 3;
const DINO_X_POS: u16 = 5;

const DINO_CHAR_RUNNING: &str = "😏";
const DINO_CHAR_JUMPING: &str = "😯";
const OBSTACLE_CHAR: &str = "💩";
const DINO_CHAR_GAME_OVER: &str = "😵";

const INITIAL_JUMP_VELOCITY: f32 = -1.6;
const GRAVITY: f32 = 0.4;

const OBSTACLE_MIN_SPAWN_GAP_FRAMES: u16 = 25;
const OBSTACLE_MAX_SPAWN_GAP_FRAMES: u16 = 45;

const DEFAULT_GAME_TICK_MS: u64 = 80;
const FASTER_GAME_TICK_MS: u64 = 60;
const SPEED_INCREASE_THRESHOLD: u32 = 500;

struct Dino {
    y_pos: f32,
    velocity_y: f32,
    is_jumping: bool,
}

impl Dino {
    fn new() -> Self {
        Dino {
            y_pos: GROUND_Y as f32,
            velocity_y: 0.0,
            is_jumping: false,
        }
    }

    fn jump(&mut self) {
        if !self.is_jumping {
            self.is_jumping = true;
            self.velocity_y = INITIAL_JUMP_VELOCITY;
        }
    }

    fn update(&mut self) {
        if self.is_jumping {
            self.y_pos += self.velocity_y;
            self.velocity_y += GRAVITY;

            if self.y_pos >= GROUND_Y as f32 {
                self.y_pos = GROUND_Y as f32;
                self.is_jumping = false;
                self.velocity_y = 0.0;
            }
        }
    }

    fn display_char(&self, is_game_over: bool) -> &'static str {
        if is_game_over {
            DINO_CHAR_GAME_OVER
        } else if self.is_jumping {
            DINO_CHAR_JUMPING
        } else {
            DINO_CHAR_RUNNING
        }
    }

    fn screen_y(&self) -> u16 {
        self.y_pos.round() as u16
    }
}

struct Obstacle {
    x_pos: u16,
}

impl Obstacle {
    fn new(x: u16) -> Self {
        Obstacle { x_pos: x }
    }
}

struct GameState {
    dino: Dino,
    obstacles: Vec<Obstacle>,
    score: u32,
    game_over: bool,
    frames_until_next_obstacle: u16,
    rng: rand::rngs::ThreadRng,
    current_game_tick_ms: u64,
    speed_boost_applied: bool,
}

impl GameState {
    fn new() -> Self {
        let mut rng = rand::thread_rng();
        GameState {
            dino: Dino::new(),
            obstacles: Vec::new(),
            score: 0,
            game_over: false,
            frames_until_next_obstacle: rng.gen_range(OBSTACLE_MIN_SPAWN_GAP_FRAMES..=OBSTACLE_MAX_SPAWN_GAP_FRAMES),
            current_game_tick_ms: DEFAULT_GAME_TICK_MS,
            speed_boost_applied: false,
            rng,
        }
    }

    fn update_tick(&mut self) {
        if self.game_over {
            return;
        }

        self.dino.update();
        self.score += 1;

        if !self.speed_boost_applied && self.score >= SPEED_INCREASE_THRESHOLD {
            self.current_game_tick_ms = FASTER_GAME_TICK_MS;
            self.speed_boost_applied = true;
        }

        self.obstacles.retain_mut(|obs| {
            if obs.x_pos > 0 {
                obs.x_pos -= 1;
                true
            } else {
                false
            }
        });

        if self.frames_until_next_obstacle == 0 {
            self.obstacles.push(Obstacle::new(SCREEN_WIDTH - 1));
            self.frames_until_next_obstacle = self.rng.gen_range(OBSTACLE_MIN_SPAWN_GAP_FRAMES..=OBSTACLE_MAX_SPAWN_GAP_FRAMES);
        } else {
            self.frames_until_next_obstacle -= 1;
        }
        for obstacle in &self.obstacles {
            if obstacle.x_pos == DINO_X_POS && self.dino.screen_y() == GROUND_Y {
                self.game_over = true;
                break;
            }
        }
    }
}

fn render(stdout: &mut Stdout, game_state: &GameState) -> std::io::Result<()> {
    execute!(stdout, Clear(ClearType::All), MoveTo(0, 0))?;

    for x in 0..SCREEN_WIDTH {
        execute!(stdout, MoveTo(x, GROUND_Y + 1), Print('-'.stylize().cyan()))?;
    }

    execute!(
        stdout,
        MoveTo(DINO_X_POS, game_state.dino.screen_y()),
        Print(game_state.dino.display_char(game_state.game_over))
    )?;

    for obstacle in &game_state.obstacles {
        if obstacle.x_pos < SCREEN_WIDTH {
            execute!(stdout, MoveTo(obstacle.x_pos, GROUND_Y), Print(OBSTACLE_CHAR))?;
        }
    }

    let score_text = format!("Score: {}", game_state.score);
    execute!(stdout, MoveTo(0, 0), Print(score_text))?;

    if game_state.game_over {
        let game_over_msg = "GAME OVER! (r: restart, q: quit)";
        let msg_x = (SCREEN_WIDTH.saturating_sub(game_over_msg.len() as u16)) / 2;
        let msg_y = SCREEN_HEIGHT / 2;
        execute!(stdout, MoveTo(msg_x, msg_y), Print(game_over_msg.stylize().red()))?;
    }

    stdout.flush()
}

fn main() -> std::io::Result<()> {
    let mut stdout = stdout();
    execute!(stdout, EnterAlternateScreen, Hide)?;
    enable_raw_mode()?;

    let mut game_state = GameState::new();

    loop {
        if poll(Duration::from_millis(10))? {
            if let Event::Key(KeyEvent { code, modifiers, .. }) = read()? {
                match code {
                    KeyCode::Char('q') => break,
                    KeyCode::Char('c') if modifiers == KeyModifiers::CONTROL => break,
                    KeyCode::Char(' ') if !game_state.game_over => game_state.dino.jump(),
                    KeyCode::Char('r') if game_state.game_over => {
                        game_state = GameState::new();
                    }
                    _ => {}
                }
            }
        }

        if !game_state.game_over {
            game_state.update_tick();
        }

        render(&mut stdout, &game_state)?;

        if !game_state.game_over {
            thread::sleep(Duration::from_millis(game_state.current_game_tick_ms));
        } else {
            thread::sleep(Duration::from_millis(50));
        }
    }

    execute!(stdout, Show, LeaveAlternateScreen)?;
    disable_raw_mode()?;

    Ok(())
}