import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'

export interface ComboOption { key: string; label: string; sub?: string }

/**
 * Text input with a suggestion list. Free text is kept (for "Handled by" names without an account);
 * choosing a suggestion calls `onSelect`.
 */
export function Combobox({ value, onChange, options, onSelect, placeholder, required, emptyText = 'No matches', loading, id, autoFocus }: {
  value: string; onChange: (value: string) => void; options: ComboOption[]; onSelect: (option: ComboOption) => void
  placeholder?: string; required?: boolean; emptyText?: string; loading?: boolean; id?: string; autoFocus?: boolean
}) {
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const listId = useId()
  const wrap = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const close = (event: MouseEvent) => { if (!wrap.current?.contains(event.target as Node)) setOpen(false) }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [])
  const choose = (option: ComboOption) => { onSelect(option); setOpen(false) }
  const keyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown') { event.preventDefault(); setOpen(true); setActive((index) => Math.min(index + 1, options.length - 1)) }
    else if (event.key === 'ArrowUp') { event.preventDefault(); setActive((index) => Math.max(index - 1, 0)) }
    else if (event.key === 'Enter' && open && options[active]) { event.preventDefault(); event.stopPropagation(); choose(options[active]) }
    else if (event.key === 'Escape' && open) { event.stopPropagation(); setOpen(false) }
  }
  return <div className="combo" ref={wrap}>
    <input id={id} role="combobox" aria-expanded={open} aria-controls={listId} aria-autocomplete="list" autoComplete="off" autoFocus={autoFocus}
      required={required} value={value} placeholder={placeholder}
      onChange={(event) => { onChange(event.target.value); setOpen(true); setActive(0) }} onFocus={() => setOpen(true)} onKeyDown={keyDown} />
    {open && (options.length > 0 || loading || value.trim().length > 1) && <div className="combo-list" id={listId} role="listbox">
      {loading ? <div className="combo-empty">Searching…</div> : options.length === 0 ? <div className="combo-empty">{emptyText}</div> :
        options.map((option, index) => <button type="button" role="option" key={option.key} aria-selected={index === active} className="combo-option" onMouseEnter={() => setActive(index)} onMouseDown={(event) => event.preventDefault()} onClick={() => choose(option)}><strong>{option.label}</strong>{option.sub && <span>{option.sub}</span>}</button>)}
    </div>}
  </div>
}
