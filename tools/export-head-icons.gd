extends SceneTree

# Run with the project's MegaDot: --headless --path . --script tools/export-head-icons.gd
# Copy the rig's actual open-eye head pixels; never redraw, rescale or recolor the face.
const ASSET_DIR = "res://Shenshen/images/character_forms/human/p1/"

func _initialize():
    var rig = JSON.parse_string(FileAccess.get_file_as_string(ASSET_DIR + "rig.json"))
    var head = rig.parts.filter(func(part): return part.id == "head")[0]
    var region = Rect2i(head.region[0], head.region[1], head.region[2], head.region[3])
    var source = Image.load_from_file(ProjectSettings.globalize_path(ASSET_DIR + head.file))
    if source == null:
        push_error("Cannot read the rig head source")
        quit(1)
        return
    source.convert(Image.FORMAT_RGBA8)
    var avatar_side = maxi(320, maxi(region.size.x, region.size.y))
    var avatar_size = Vector2i(avatar_side, avatar_side)
    var button_size = Vector2i(region.size.x, ceili(region.size.x * 1.5))
    for target in [["avatar.png", avatar_size], ["select-icon.png", button_size]]:
        var canvas_size: Vector2i = target[1]
        var offset = Vector2i((canvas_size.x - region.size.x) / 2, (canvas_size.y - region.size.y) / 2)
        var result = Image.create_empty(canvas_size.x, canvas_size.y, false, Image.FORMAT_RGBA8)
        result.fill(Color(0, 0, 0, 0))
        result.blit_rect(source, region, offset)
        var error = result.save_png(ProjectSettings.globalize_path(ASSET_DIR + target[0]))
        if error != OK:
            push_error("Cannot export head icon: " + target[0])
            quit(1)
            return
        print("SHENSHEN_HEAD_ICON ", target[0], " canvas=", canvas_size, " offset=", offset, " source=", region)
    quit(0)
