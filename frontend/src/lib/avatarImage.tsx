export const ACCEPTED_AVATAR_TYPES = ['image/jpeg', 'image/png', 'image/webp']

const OUTPUT_SIZE = 256
const MAX_SOURCE_BYTES = 10 * 1024 * 1024

// Centre-crops a picture to a square and shrinks it to 256 x 256, returning a small JPEG.
// Doing this in the browser keeps uploads tiny and drops any metadata (like GPS tags) the photo carried.
export async function prepareAvatar(file: File): Promise<Blob> {
    if (!ACCEPTED_AVATAR_TYPES.includes(file.type)) {
        throw new Error('Choose a JPEG, PNG or WebP picture.')
    }
    if (file.size > MAX_SOURCE_BYTES) {
        throw new Error('That picture is too large. Choose one under 10 MB.')
    }

    let bitmap: ImageBitmap
    try {
        bitmap = await createImageBitmap(file)
    } catch {
        throw new Error("That file couldn't be read as a picture.")
    }

    const canvas = document.createElement('canvas')
    canvas.width = OUTPUT_SIZE
    canvas.height = OUTPUT_SIZE
    const context = canvas.getContext('2d')
    if (!context) {
        bitmap.close()
        throw new Error("Your browser couldn't prepare the picture.")
    }

    // White underneath, so a transparent PNG doesn't turn black when saved as a JPEG.
    context.fillStyle = '#ffffff'
    context.fillRect(0, 0, OUTPUT_SIZE, OUTPUT_SIZE)

    const side = Math.min(bitmap.width, bitmap.height)
    context.drawImage(
        bitmap,
        (bitmap.width - side) / 2,
        (bitmap.height - side) / 2,
        side,
        side,
        0,
        0,
        OUTPUT_SIZE,
        OUTPUT_SIZE,
    )
    bitmap.close()

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.85))
    if (!blob) throw new Error("Your browser couldn't prepare the picture.")
    return blob
}