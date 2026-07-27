import { useId, useRef, useState } from 'react'

type Props = {
  accept?: string
  disabled?: boolean
  label?: string
  buttonText?: string
  className?: string
  onFile: (file: File | null) => void
}

export function FileBrowseButton({
  accept,
  disabled,
  label,
  buttonText = 'Browse',
  className = '',
  onFile,
}: Props) {
  const inputId = useId()
  const inputRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)

  return (
    <div className={`file-browse ${className}`.trim()}>
      {label && (
        <label className="file-browse-label" htmlFor={inputId}>
          {label}
        </label>
      )}
      <div className="file-browse-row">
        <button
          type="button"
          className="btn file-browse-btn"
          disabled={disabled}
          onClick={() => inputRef.current?.click()}
        >
          {buttonText}
        </button>
        <span className={`file-browse-name${fileName ? '' : ' empty'}`}>
          {fileName ?? 'No file selected'}
        </span>
      </div>
      <input
        id={inputId}
        ref={inputRef}
        type="file"
        accept={accept}
        className="file-browse-input"
        disabled={disabled}
        onChange={(e) => {
          const file = e.target.files?.[0] ?? null
          setFileName(file?.name ?? null)
          onFile(file)
          e.target.value = ''
        }}
      />
    </div>
  )
}
