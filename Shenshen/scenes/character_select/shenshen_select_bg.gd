extends Control

# Cancel native overscan/zoom without changing drawing order or modulation.
var _parent_transform = Transform2D.IDENTITY
var _viewport_frame = Rect2()

func _ready():
	_fit_viewport()

func _process(_delta):
	_fit_viewport()

func _fit_viewport():
	var parent_item = get_parent() as CanvasItem
	var parent_transform = parent_item.get_global_transform() if parent_item else Transform2D.IDENTITY
	var frame = get_viewport().get_visible_rect()
	if parent_transform.is_equal_approx(_parent_transform) and frame == _viewport_frame:
		return
	_parent_transform = parent_transform
	_viewport_frame = frame
	var inverse = parent_transform.affine_inverse()
	position = inverse * frame.position
	rotation = inverse.get_rotation()
	scale = inverse.get_scale()
	size = frame.size
	# Cover narrow windows by trimming the quiet left edge, preserving the staff hand.
	var artwork = $Artwork
	var image_size = artwork.texture.get_size()
	var fit_scale = maxf(size.x / image_size.x, size.y / image_size.y)
	artwork.size = image_size * fit_scale
	artwork.position = Vector2(size.x - artwork.size.x, (size.y - artwork.size.y) * 0.5)
