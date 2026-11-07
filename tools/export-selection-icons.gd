extends SceneTree

# Copy the approved selection art's close-up without resampling or adding padding.
# --headless --path . --script tools/export-selection-icons.gd
const ASSET_DIR = "res://Shenshen/images/character_select/apricot-dawn-gold/"

func _initialize():
	var layout = JSON.parse_string(FileAccess.get_file_as_string(ASSET_DIR + "selection.json"))
	if not layout is Dictionary:
		_fail("Missing or invalid selection.json")
		return
	var source = Image.load_from_file(ProjectSettings.globalize_path(ASSET_DIR + str(layout.image)))
	if source == null or source.is_empty():
		_fail("Cannot read approved selection background")
		return
	if source.get_size() != Vector2i(int(layout.design_size[0]), int(layout.design_size[1])):
		_fail("Approved background dimensions do not match selection.json")
		return
	var crop: Array = layout.select_icon_region
	var region = Rect2i(int(crop[0]), int(crop[1]), int(crop[2]), int(crop[3]))
	if not region.has_area() or not Rect2i(Vector2i.ZERO, source.get_size()).encloses(region):
		_fail("select_icon_region must be inside the approved background")
		return
	if region.size.x * 65 != region.size.y * 44:
		_fail("Selection crop must match the native 132:195 portrait ratio")
		return
	var icon = source.get_region(region)
	var error = icon.save_png(ProjectSettings.globalize_path(ASSET_DIR + "select-icon.png"))
	if error != OK:
		_fail("Cannot save selection icon: " + str(error))
		return
	print("SHENSHEN_SELECTION_ICON source=", layout.image, " region=", region, " size=", icon.get_size())
	quit(0)

func _fail(message: String):
	push_error(message)
	quit(1)
