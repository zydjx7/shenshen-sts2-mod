extends SceneTree

# Run with the project's MegaDot: --headless --path . --script tools/export-selection-portrait.gd
# Import the project's textures first; missing sources must not overwrite a valid portrait.
# Assemble the selected character from the same original parts and rig as combat.
# This produces a static Godot subscene, never a separately repainted face or body.
const ASSET_DIR = "res://Shenshen/images/character_forms/human/mystery/"
const OUTPUT = "res://Shenshen/scenes/character_select/shenshen_portrait.tscn"
const FRAME_SIZE = Vector2(633.334, 950)
const CONTENT_PADDING = Vector2(8, 24)

var _rig: Dictionary
var _root: Node2D
var _canvas: Node2D
var _parts: Dictionary = {}
var _pivots: Dictionary = {}
var _sprites: Dictionary = {}
var _draw_order: Dictionary = {}

func _initialize():
    _rig = JSON.parse_string(FileAccess.get_file_as_string(ASSET_DIR + "rig.json"))
    for part in _rig.parts:
        if not load(ASSET_DIR + part.file) is Texture2D:
            push_error("Import the rig's source textures before exporting: " + ASSET_DIR + part.file)
            quit(1)
            return
    _root = Node2D.new()
    _root.name = "Rig"
    _root.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
    _canvas = Node2D.new()
    _canvas.name = "Canvas"
    _root.add_child(_canvas)
    _canvas.owner = _root
    for part in _rig.parts:
        _parts[part.id] = part
    for id in _parts:
        _add_part(id)
    for parent in [_canvas] + _pivots.values():
        var children = parent.get_children()
        children.sort_custom(func(a, b): return _draw_order.get(a, 0) < _draw_order.get(b, 0))
        for index in children.size():
            parent.move_child(children[index], index)

    # Fit the real visible part bounds, so a wide hat or a bare foot is not cropped.
    var bounds: Rect2
    var initialized = false
    for sprite: Sprite2D in _sprites.values():
        if not sprite.visible:
            continue
        var transform = sprite.transform
        var ancestor = sprite.get_parent() as Node2D
        while ancestor != _root:
            transform = ancestor.transform * transform
            ancestor = ancestor.get_parent() as Node2D
        var part_bounds = transform * sprite.get_rect()
        bounds = bounds.merge(part_bounds) if initialized else part_bounds
        initialized = true
    if not initialized or bounds.size.x <= 0 or bounds.size.y <= 0:
        push_error("Selection portrait has no visible character parts")
        quit(1)
        return
    var content_size = FRAME_SIZE - CONTENT_PADDING * 2
    var fit_scale = minf(content_size.x / bounds.size.x, content_size.y / bounds.size.y)
    _root.scale = Vector2.ONE * fit_scale
    _root.position = CONTENT_PADDING + (content_size - bounds.size * fit_scale) / 2 - bounds.position * fit_scale
    var packed = PackedScene.new()
    var error = packed.pack(_root)
    if error == OK:
        error = ResourceSaver.save(packed, OUTPUT)
    if error != OK:
        push_error("Cannot export selection portrait: " + str(error))
        quit(1)
        return
    print("SHENSHEN_SELECTION_PORTRAIT ", OUTPUT, " bounds=", bounds, " scale=", fit_scale)
    _root.free()
    quit(0)

func _add_part(id: String) -> Node2D:
    if _pivots.has(id):
        return _pivots[id]
    var part: Dictionary = _parts[id]
    var parent: Node2D = _canvas if part.get("parent", "").is_empty() else _add_part(part.parent)
    var pivot = Node2D.new()
    pivot.name = "Part_" + id
    pivot.position = Vector2(part.position[0], part.position[1])
    pivot.rotation_degrees = part.get("rotation", 0)
    parent.add_child(pivot)
    pivot.owner = _root
    var source = load(ASSET_DIR + part.file) as Texture2D
    var texture = AtlasTexture.new()
    texture.atlas = source
    texture.region = Rect2(part.region[0], part.region[1], part.region[2], part.region[3])
    texture.filter_clip = true
    var sprite = Sprite2D.new()
    sprite.name = "Sprite"
    sprite.texture = texture
    sprite.centered = false
    sprite.offset = -Vector2(part.pivot[0], part.pivot[1]) * texture.get_size()
    sprite.scale = Vector2(part.size[0], part.size[1]) / texture.get_size()
    sprite.visible = id not in ["head_closed", "magic"]
    pivot.add_child(sprite)
    sprite.owner = _root
    _pivots[id] = pivot
    _sprites[id] = sprite
    _draw_order[pivot] = part.get("z", 0)
    _draw_order[sprite] = part.get("z", 0)
    return pivot
