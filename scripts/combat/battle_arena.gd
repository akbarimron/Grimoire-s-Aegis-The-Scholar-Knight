extends Control

# Grimoire's Aegis: The Scholar Knight - Battle Arena Controller
# Full implementation of Player Turn, Enemy Active Defend System, and Educational Evaluation.

signal turn_cycle_completed

enum BattleState {
	PLAYER_TURN,
	PLAYER_ACTION_ANIMATING,
	ENEMY_WINDUP,
	QUESTION_ACTIVE,
	QUESTION_RESOLVING,
	VICTORY,
	DEFEAT
}

var current_state: BattleState = BattleState.PLAYER_TURN

# Current Monster Data
var current_monster: Dictionary = {
	"id": "slime",
	"name": "Corrupted Slime",
	"title": "Slime Kekacauan Indeks (Prolog)",
	"max_hp": 60,
	"current_hp": 60,
	"atk": 8,
	"def": 2,
	"exp": 20,
	"gold": 25,
	"sprite": "res://assets/sprites/monsters/corrupted_slime.png"
}

# Active Question
var active_q: Dictionary = {}
var question_timer: float = 0.0
var question_max_time: float = 10.0
var reading_delay: float = 1.5
var is_reading_phase: bool = true

# UI References
@onready var label_monster_name: Label = $ArenaLayout/TopPanel/MonsterInfo/LabelName
@onready var label_monster_title: Label = $ArenaLayout/TopPanel/MonsterInfo/LabelTitle
@onready var bar_monster_hp: ProgressBar = $ArenaLayout/TopPanel/MonsterInfo/BarHP
@onready var tex_monster_sprite: TextureRect = $ArenaLayout/CombatStage/MonsterNode/MonsterSprite

@onready var label_player_hp: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelHP
@onready var bar_player_hp: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarHP
@onready var label_player_sta: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelStamina
@onready var bar_player_sta: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarStamina
@onready var label_player_mem: Label = $ArenaLayout/BottomPanel/PlayerStats/LabelMemory
@onready var bar_player_mem: ProgressBar = $ArenaLayout/BottomPanel/PlayerStats/BarMemory
@onready var label_fragments: Label = $ArenaLayout/TopPanel/FragmentCounter/LabelCount

# Action Menu
@onready var action_menu: VBoxContainer = $ArenaLayout/BottomPanel/ActionMenu
@onready var btn_attack: Button = $ArenaLayout/BottomPanel/ActionMenu/TopActions/BtnAttack
@onready var btn_spell: Button = $ArenaLayout/BottomPanel/ActionMenu/TopActions/BtnSpell
@onready var btn_analyze: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnAnalyze
@onready var btn_inventory: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnInventory
@onready var btn_flee: Button = $ArenaLayout/BottomPanel/ActionMenu/BottomActions/BtnFlee

# Question Dialog Pop-up
@onready var question_panel: PanelContainer = $QuestionOverlay
@onready var label_q_title: Label = $QuestionOverlay/Margin/VBox/LabelQTitle
@onready var label_q_body: Label = $QuestionOverlay/Margin/VBox/LabelQBody
@onready var array_container: HBoxContainer = $QuestionOverlay/Margin/VBox/ArrayContainer
@onready var options_container: GridContainer = $QuestionOverlay/Margin/VBox/OptionsGrid
@onready var bar_timer: ProgressBar = $QuestionOverlay/Margin/VBox/TimerBar
@onready var label_feedback: Label = $QuestionOverlay/Margin/VBox/LabelFeedback

# Visual FX & Shields
@onready var aegis_shield: TextureRect = $ArenaLayout/CombatStage/PlayerNode/AegisShieldFX
@onready var label_combat_log: Label = $ArenaLayout/LogPanel/LabelLog
@onready var anim_player: AnimationPlayer = $CombatAnimationPlayer

func _ready() -> void:
	_connect_buttons()
	_load_monster("slime")
	_update_ui()
	GameManager.stats_changed.connect(_update_ui)
	question_panel.visible = false
	aegis_shield.visible = false
	_log("Pertarungan dimulai! Scholar Knight berhadapan dengan " + current_monster["name"] + ".")

func _connect_buttons() -> void:
	btn_attack.pressed.connect(_on_attack_pressed)
	btn_spell.pressed.connect(_on_spell_pressed)
	btn_analyze.pressed.connect(_on_analyze_pressed)
	btn_inventory.pressed.connect(_on_inventory_pressed)
	btn_flee.pressed.connect(_on_flee_pressed)

func _process(delta: float) -> void:
	if current_state == BattleState.QUESTION_ACTIVE:
		if is_reading_phase:
			reading_delay -= delta
			bar_timer.value = 100.0
			label_feedback.text = "Masa Baca: %.1f dtk..." % max(0.0, reading_delay)
			if reading_delay <= 0.0:
				is_reading_phase = false
				label_feedback.text = "PILIH JAWABAN ANDA!"
		else:
			question_timer -= delta
			var progress_pct: float = (question_timer / question_max_time) * 100.0
			bar_timer.value = progress_pct
			
			# Color feedback per GDD section 17
			if progress_pct >= 60.0:
				bar_timer.modulate = Color(0.1, 0.9, 1.0) # Blue / Cyan
			elif progress_pct > 30.0:
				bar_timer.modulate = Color(1.0, 0.8, 0.2) # Yellow / Amber
			else:
				bar_timer.modulate = Color(1.0, 0.2, 0.2) # Red Panic
				
			if question_timer <= 0.0:
				_resolve_answer("TIMEOUT_NO_ANSWER")

func _load_monster(monster_type: String) -> void:
	match monster_type:
		"slime":
			current_monster = {
				"id": "slime",
				"name": "Corrupted Slime",
				"title": "Slime Anomali Indeks (Prolog)",
				"max_hp": 50,
				"current_hp": 50,
				"atk": 9,
				"def": 2,
				"exp": 20,
				"gold": 25,
				"sprite": "res://assets/sprites/monsters/corrupted_slime.png"
			}
		"skeleton_archer":
			current_monster = {
				"id": "skeleton_archer",
				"name": "Skeleton Archer",
				"title": "Pemanah Benteng Tulang (Act II: Bubble Sort)",
				"max_hp": 75,
				"current_hp": 75,
				"atk": 14,
				"def": 4,
				"exp": 45,
				"gold": 50,
				"sprite": "res://assets/sprites/monsters/skeleton_archer.png"
			}
		"lost_wisp":
			current_monster = {
				"id": "lost_wisp",
				"name": "Lost Wisp",
				"title": "Hantu Lentera (Act I: Linear Search)",
				"max_hp": 60,
				"current_hp": 60,
				"atk": 11,
				"def": 3,
				"exp": 30,
				"gold": 35,
				"sprite": "res://assets/sprites/monsters/lost_wisp.png"
			}
	tex_monster_sprite.texture = load(current_monster["sprite"])
	_update_ui()

func _update_ui() -> void:
	# Monster UI
	label_monster_name.text = current_monster["name"]
	label_monster_title.text = current_monster["title"]
	bar_monster_hp.max_value = current_monster["max_hp"]
	bar_monster_hp.value = current_monster["current_hp"]
	
	# Player UI
	label_player_hp.text = "HP: %d / %d" % [GameManager.current_hp, GameManager.max_hp]
	bar_player_hp.max_value = GameManager.max_hp
	bar_player_hp.value = GameManager.current_hp
	
	label_player_sta.text = "STA: %d / %d" % [GameManager.current_stamina, GameManager.max_stamina]
	bar_player_sta.max_value = GameManager.max_stamina
	bar_player_sta.value = GameManager.current_stamina
	
	label_player_mem.text = "MEM: %d / %d" % [GameManager.current_memory, GameManager.max_memory]
	bar_player_mem.max_value = GameManager.max_memory
	bar_player_mem.value = GameManager.current_memory
	
	label_fragments.text = "Logic Fragments: %d  |  Gold: %d" % [GameManager.logic_fragments, GameManager.gold]
	
	# Action buttons availability
	btn_attack.disabled = (GameManager.current_stamina < 10 or current_state != BattleState.PLAYER_TURN)
	btn_spell.disabled = (GameManager.current_memory < 8 or current_state != BattleState.PLAYER_TURN)
	btn_inventory.disabled = (current_state != BattleState.PLAYER_TURN)
	btn_analyze.disabled = (current_state != BattleState.PLAYER_TURN)
	btn_flee.disabled = (current_state != BattleState.PLAYER_TURN)

func _log(msg: String) -> void:
	label_combat_log.text = msg

# --- PLAYER ACTIONS ---

func _on_attack_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or GameManager.current_stamina < 10:
		return
	GameManager.current_stamina -= 10
	current_state = BattleState.PLAYER_ACTION_ANIMATING
	_update_ui()
	
	# Damage Formula: max(1, ATK - DEF) * random(0.9, 1.1)
	var raw_dmg: float = max(1.0, float(GameManager.attack_power - current_monster["def"]))
	var final_dmg: int = int(raw_dmg * randf_range(0.95, 1.1))
	current_monster["current_hp"] = max(0, current_monster["current_hp"] - final_dmg)
	_log("Tebasan Pedang Ordo mengenai %s sebesar %d damage!" % [current_monster["name"], final_dmg])
	_update_ui()
	
	if current_monster["current_hp"] <= 0:
		_handle_victory()
	else:
		await get_tree().create_timer(1.0).timeout
		_start_enemy_turn()

func _on_spell_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN or GameManager.current_memory < 8:
		return
	GameManager.current_memory -= 8
	current_state = BattleState.PLAYER_ACTION_ANIMATING
	_update_ui()
	
	# Spell Linear Strike
	var spell_power: int = 22
	var final_dmg: int = int(spell_power * (1.0 + float(GameManager.intelligence) / 20.0))
	current_monster["current_hp"] = max(0, current_monster["current_hp"] - final_dmg)
	_log("Mantra Grimoire: LINEAR STRIKE membelah barisan monster sebesar %d damage sihir!" % final_dmg)
	_update_ui()
	
	if current_monster["current_hp"] <= 0:
		_handle_victory()
	else:
		await get_tree().create_timer(1.2).timeout
		_start_enemy_turn()

func _on_analyze_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN:
		return
	_log("ANALISIS CODEX: %s\nKelemahan: Algoritma Pencarian & Pengurutan runtuh saat ditangkis presisi." % current_monster["title"])
	await get_tree().create_timer(1.8).timeout
	_start_enemy_turn()

func _on_inventory_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN:
		return
	if GameManager.inventory.get("potion", 0) > 0:
		GameManager.inventory["potion"] -= 1
		GameManager.heal_hp(40)
		_log("Meminum Potion! Pulih 40 HP.")
	else:
		_log("Potion habis!")
		return
	await get_tree().create_timer(1.0).timeout
	_start_enemy_turn()

func _on_flee_pressed() -> void:
	if current_state != BattleState.PLAYER_TURN:
		return
	if randf() <= 0.4:
		_log("Berhasil kabur dari pertarungan!")
		current_state = BattleState.DEFEAT
	else:
		_log("Gagal melarikan diri! Serangan monster mendekat!")
		await get_tree().create_timer(1.0).timeout
		_start_enemy_turn()

# --- ENEMY ACTIVE DEFEND SYSTEM ---

func _start_enemy_turn() -> void:
	current_state = BattleState.ENEMY_WINDUP
	_log("PERINGATAN! %s bersiap melancarkan serangan Glitch!" % current_monster["name"])
	
	# Bullet-time delay 0.6s per GDD section 7
	await get_tree().create_timer(0.6).timeout
	
	# Generate Question
	active_q = QuestionGenerator.generate_question_for_monster(current_monster["id"])
	question_max_time = active_q.get("timer", 8.0)
	question_timer = question_max_time
	reading_delay = 1.5
	is_reading_phase = true
	
	_show_question_popup(active_q)
	current_state = BattleState.QUESTION_ACTIVE

func _show_question_popup(q: Dictionary) -> void:
	question_panel.visible = true
	label_q_title.text = "[ %s ] - %s" % [q.get("tp", "TP-01"), q.get("title", "Tantangan Logika")]
	label_q_body.text = q.get("question", "")
	label_feedback.text = "Masa Baca: 1.5 dtk..."
	
	# Render interactive array elements if present
	for child in array_container.get_children():
		child.queue_free()
	
	if q.has("array") and q["array"] is Array:
		var arr: Array = q["array"]
		for i in range(arr.size()):
			var card: VBoxContainer = VBoxContainer.new()
			var val_lbl: Label = Label.new()
			val_lbl.text = str(arr[i])
			val_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			
			var idx_lbl: Label = Label.new()
			idx_lbl.text = "[ %d ]" % i
			idx_lbl.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			idx_lbl.modulate = Color(0.4, 0.8, 1.0)
			
			card.add_child(val_lbl)
			card.add_child(idx_lbl)
			array_container.add_child(card)

	# Render Options Buttons
	for child in options_container.get_children():
		child.queue_free()
		
	var opts: Array = q.get("options", [])
	# Panic level 3 shuffles buttons (GDD section 15)
	if GameManager.panic_level >= 3:
		opts.shuffle()
		
	for i in range(opts.size()):
		var opt = opts[i]
		var btn: Button = Button.new()
		btn.text = "[%d] %s" % [i + 1, str(opt)]
		btn.custom_minimum_size = Vector2(170, 32)
		btn.add_theme_font_size_override("font_size", 11)
		
		# Stylized medieval pixel button
		var sb: StyleBoxFlat = StyleBoxFlat.new()
		sb.bg_color = Color(0.16, 0.20, 0.27, 1.0)
		sb.border_width_left = 1
		sb.border_width_top = 1
		sb.border_width_right = 1
		sb.border_width_bottom = 1
		sb.border_color = Color(0.4, 0.6, 0.8, 1.0)
		sb.set_corner_radius_all(3)
		sb.content_margin_left = 6
		sb.content_margin_right = 6
		btn.add_theme_stylebox_override("normal", sb)
		
		var sb_hover: StyleBoxFlat = sb.duplicate()
		sb_hover.bg_color = Color(0.24, 0.32, 0.44, 1.0)
		sb_hover.border_color = Color(0.2, 0.9, 1.0, 1.0)
		btn.add_theme_stylebox_override("hover", sb_hover)
		
		btn.pressed.connect(func(): _resolve_answer(str(opt)))
		options_container.add_child(btn)

func _unhandled_input(event: InputEvent) -> void:
	if not (event is InputEventKey) or not event.is_pressed() or event.is_echo():
		return
		
	# Keyboard shortcuts 1-5
	var key: int = event.keycode
	if current_state == BattleState.PLAYER_TURN:
		match key:
			KEY_1:
				_on_attack_pressed()
			KEY_2:
				_on_spell_pressed()
			KEY_3:
				_on_analyze_pressed()
			KEY_4:
				_on_inventory_pressed()
			KEY_5:
				_on_flee_pressed()
	elif current_state == BattleState.QUESTION_ACTIVE and not is_reading_phase:
		var opts: Array = active_q.get("options", [])
		var idx: int = -1
		match key:
			KEY_1: idx = 0
			KEY_2: idx = 1
			KEY_3: idx = 2
			KEY_4: idx = 3
		if idx >= 0 and idx < opts.size():
			_resolve_answer(str(opts[idx]))

func _resolve_answer(chosen_answer: String) -> void:
	current_state = BattleState.QUESTION_RESOLVING
	var correct: String = active_q.get("correct_answer", "")
	var is_correct: bool = (chosen_answer == correct)
	var time_left_ratio: float = question_timer / question_max_time
	var explanation: String = active_q.get("explanation", "")
	
	# Evaluation of 3 Tiers (GDD Section 7)
	if is_correct and time_left_ratio >= 0.6:
		# TIER 1: PERFECT GUARD
		_handle_perfect_guard(explanation)
	elif is_correct:
		# TIER 2: GUARD
		_handle_guard(explanation)
	else:
		# TIER 3: BREAK (Lethal Strike)
		_handle_break(chosen_answer, explanation)

func _handle_perfect_guard(explanation: String) -> void:
	_log("AEGIS ACTIVE! PERFECT GUARD! Serangan musuh dipantulkan!")
	label_feedback.text = "PERFECT GUARD! (+1 Logic Fragment, Counter Attack!)\n%s" % explanation
	label_feedback.modulate = Color(0.2, 1.0, 0.4)
	
	# Visual shield
	aegis_shield.visible = true
	GameManager.clear_one_panic()
	GameManager.add_reward(0, 1, 10)
	
	# Counter Attack: 50% player ATK damage to monster
	var counter_dmg: int = max(1, int(float(GameManager.attack_power) * 0.5))
	current_monster["current_hp"] = max(0, current_monster["current_hp"] - counter_dmg)
	_update_ui()
	
	await get_tree().create_timer(2.2).timeout
	aegis_shield.visible = false
	question_panel.visible = false
	
	if current_monster["current_hp"] <= 0:
		_handle_victory()
	else:
		_end_turn_and_start_player()

func _handle_guard(explanation: String) -> void:
	_log("GUARD BERHASIL! Perisai menahan serangan (0 Damage).")
	label_feedback.text = "GUARD SUKSES! (0 Damage)\n%s" % explanation
	label_feedback.modulate = Color(0.4, 0.8, 1.0)
	
	await get_tree().create_timer(2.0).timeout
	question_panel.visible = false
	_end_turn_and_start_player()

func _handle_break(chosen: String, explanation: String) -> void:
	# Damage Break Formula (GDD Section 7):
	# Damage = min(3 * ATK musuh - DEF pemain, 0.5 * HP maks)
	var raw_break: int = (3 * current_monster["atk"]) - GameManager.defense_power
	var max_cap: int = int(float(GameManager.max_hp) * 0.5)
	if GameManager.current_mode == GameManager.DifficultyMode.STORY:
		max_cap = int(float(GameManager.max_hp) * 0.25)
	var final_break_dmg: int = clampi(raw_break, 5, max_cap)
	
	GameManager.take_damage(final_break_dmg)
	GameManager.add_panic(1)
	
	_log("BREAK! Pertahanan hancur! Terkena %d damage + Panic bertambah." % final_break_dmg)
	label_feedback.text = "BREAK! Jawaban: %s. Kunci: %s\n%s" % [chosen, active_q.get("correct_answer", ""), explanation]
	label_feedback.modulate = Color(1.0, 0.3, 0.3)
	_update_ui()
	
	await get_tree().create_timer(2.5).timeout
	question_panel.visible = false
	
	if GameManager.current_hp <= 0:
		_handle_defeat()
	else:
		_end_turn_and_start_player()

func _end_turn_and_start_player() -> void:
	GameManager.restore_turn_stamina()
	current_state = BattleState.PLAYER_TURN
	_update_ui()
	_log("Giliran Pemain! Stamina pulih +15. Pilih tindakan Anda.")

func _handle_victory() -> void:
	current_state = BattleState.VICTORY
	GameManager.add_reward(current_monster["gold"], 1, current_monster["exp"])
	_log("KEMENANGAN! %s berhasil dimurnikan! Mendapatkan %d Gold dan EXP." % [current_monster["name"], current_monster["gold"]])
	await get_tree().create_timer(2.0).timeout
	# Switch to next monster challenge to demonstrate progression
	if current_monster["id"] == "slime":
		_load_monster("skeleton_archer")
		current_state = BattleState.PLAYER_TURN
		_log("Monster baru mendekat: Skeleton Archer!")
	elif current_monster["id"] == "skeleton_archer":
		_load_monster("lost_wisp")
		current_state = BattleState.PLAYER_TURN
		_log("Monster baru mendekat: Lost Wisp!")
	else:
		_log("Semua monster pengujian berhasil dikalahkan! Grimoire tersusun kembali.")

func _handle_defeat() -> void:
	current_state = BattleState.DEFEAT
	_log("KSATRIA TUMBANG... Grimoire meredup. Tekan R untuk mengulang pertarungan.")
