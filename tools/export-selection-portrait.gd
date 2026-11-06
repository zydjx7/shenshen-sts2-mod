extends SceneTree

# Run after importing textures with the project's MegaDot:
# --headless --path . --script tools/export-selection-portrait.gd
# The portrait and icons retain the approved source pixels; only the shared halo is separate.
const ASSET_DIR = "res://Shenshen/images/character_forms/human/solar-b5/"
const RING_SCENE = ASSET_DIR + "solar-ring.tscn"
const OUTPUT = "res://Shenshen/scenes/character_select/shenshen_portrait.tscn"
const FRAME_SIZE = Vector2(633.334, 950)
const CONTENT_PADDING = Vector2(8, 24)
const ALPHA_THRESHOLD = 0.04
const AVATAR_SIZE = Vector2i(450, 450)
const SELECT_ICON_SIZE = Vector2i(450, 675)

func _initialize():
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(ASSET_DIR + "rig.json"))
	if not parsed is Dictionary:
		_fail("Missing or invalid solar-b5 rig.json")
		return
	var rig: Dictionary = parsed
	var image_path: String = ASSET_DIR + str(rig.image)
	var texture = load(image_path) as Texture2D
	var source = Image.load_from_file(image_path)
	var ring_scene = load(RING_SCENE) as PackedScene
	if texture == null or source == null or source.is_empty() or ring_scene == null:
		_fail("Import the approved character texture and shared solar ring before exporting")
		return
	var design_size = Vector2i(int(rig.design_size[0]), int(rig.design_size[1]))
	if source.get_size() != design_size:
		_fail("Approved character texture dimensions do not match rig design_size")
		return
	var head: Array = rig.head_region
	var region = Rect2i(int(head[0]), int(head[1]), int(head[2]), int(head[3]))
	if not Rect2i(Vector2i.ZERO, source.get_size()).encloses(region) or region.size.x <= 0 or region.size.y <= 0:
		_fail("head_region must be a nonempty region inside the approved image")
		return
	if region.size.x > AVATAR_SIZE.x or region.size.y > AVATAR_SIZE.y:
		_fail("head_region must fit the avatar without scaling approved pixels")
		return
	var character_bounds = _alpha_bounds(source)
	if not character_bounds.has_area():
		_fail("Approved character texture has no visible pixels")
		return

	var root = Node2D.new()
	root.name = "Rig"
	root.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	var canvas = Node2D.new()
	canvas.name = "Canvas"
	root.add_child(canvas)
	canvas.owner = root

	# Instantiate the same scene as combat. Scene ownership preserves it as an instance.
	var ring = ring_scene.instantiate() as Node2D
	ring.name = "SolarRing"
	ring.position = Vector2(float(rig.halo_center[0]), float(rig.halo_center[1]))
	ring.scale = Vector2.ONE * (float(rig.halo_radius) / 250.0)
	canvas.add_child(ring)
	ring.owner = root

	var character = Sprite2D.new()
	character.name = "Character"
	character.texture = texture
	character.centered = false
	canvas.add_child(character)
	character.owner = root

	# Fit actual alpha > 0.04, excluding near-transparent cutout haze.
	# Include every shared halo line, so its long cardinal ticks are not clipped.
	var bounds = character_bounds.merge(_line_bounds(ring))
	var content_size = FRAME_SIZE - CONTENT_PADDING * 2
	var fit_scale = minf(content_size.x / bounds.size.x, content_size.y / bounds.size.y)
	root.scale = Vector2.ONE * fit_scale
	root.position = CONTENT_PADDING + (content_size - bounds.size * fit_scale) / 2 - bounds.position * fit_scale

	var packed = PackedScene.new()
	var error = packed.pack(root)
	if error == OK:
		error = ResourceSaver.save(packed, OUTPUT)
	if error == OK:
		error = _save_icon(source, region, AVATAR_SIZE, ASSET_DIR + "avatar.png")
	if error == OK:
		error = _save_icon(source, region, SELECT_ICON_SIZE, ASSET_DIR + "select-icon.png")
	root.free()
	if error != OK:
		_fail("Cannot export solar portrait or icons: " + str(error))
		return
	print("SHENSHEN_SELECTION_PORTRAIT ", OUTPUT, " bounds=", bounds, " scale=", fit_scale)
	print("SHENSHEN_SELECTION_ICONS head_region=", region, " avatar=", AVATAR_SIZE, " select=", SELECT_ICON_SIZE)
	quit(0)

func _alpha_bounds(source: Image) -> Rect2:
	var minimum = source.get_size()
	var maximum = Vector2i(-1, -1)
	for y in source.get_height():
		for x in source.get_width():
			if source.get_pixel(x, y).a > ALPHA_THRESHOLD:
				minimum = minimum.min(Vector2i(x, y))
				maximum = maximum.max(Vector2i(x, y))
	if maximum.x < 0:
		return Rect2()
	return Rect2(Vector2(minimum), Vector2(maximum - minimum + Vector2i.ONE))

func _line_bounds(ring: Node2D) -> Rect2:
	var bounds = Rect2()
	var initialized = false
	for child in ring.get_children():
		var line = child as Line2D
		if line == null or not line.visible:
			continue
		for point in line.points:
			var position: Vector2 = ring.transform * line.transform * point
			var radius: float = line.width * 0.5 * ring.scale.x
			var point_bounds = Rect2(position - Vector2.ONE * radius, Vector2.ONE * radius * 2)
			bounds = bounds.merge(point_bounds) if initialized else point_bounds
			initialized = true
	return bounds

func _save_icon(source: Image, region: Rect2i, size: Vector2i, path: String) -> Error:
	var icon = Image.create_empty(size.x, size.y, false, Image.FORMAT_RGBA8)
	icon.fill(Color.TRANSPARENT)
	var offset = Vector2i((size.x - region.size.x) / 2, (size.y - region.size.y) / 2)
	# blit_rect copies source pixels, with no resampling, stretching or repainting.
	icon.blit_rect(source, region, offset)
	return icon.save_png(path)

func _fail(message: String):
	push_error(message)
	quit(1)
