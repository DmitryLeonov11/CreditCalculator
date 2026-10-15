import type { AxiosResponse } from 'axios'

export function saveDownloadedFile(
  response: AxiosResponse<Blob>,
  fallbackFileName: string,
) {
  const disposition = response.headers['content-disposition'] as
    string | undefined
  const encodedName = disposition?.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  const plainName = disposition?.match(/filename="?([^";]+)"?/i)?.[1]
  const fileName = encodedName
    ? decodeURIComponent(encodedName)
    : (plainName ?? fallbackFileName)
  const objectUrl = URL.createObjectURL(response.data)
  const anchor = document.createElement('a')

  anchor.href = objectUrl
  anchor.download = fileName
  anchor.hidden = true
  document.body.append(anchor)
  anchor.click()
  anchor.remove()
  window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000)
}
