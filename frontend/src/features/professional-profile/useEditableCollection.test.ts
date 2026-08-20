import { describe, it, expect, vi } from 'vitest'
import { act, renderHook, waitFor } from '@testing-library/react'
import { useSectionSave } from './useEditableCollection'

describe('useSectionSave', () => {
  it('reports success, clears isSaving, and keeps no error once the save resolves', async () => {
    const save = vi.fn().mockResolvedValue(undefined)
    const { result } = renderHook(() => useSectionSave(['a'], save))

    let succeeded: boolean | undefined
    await act(async () => {
      succeeded = await result.current.handleSave()
    })

    expect(succeeded).toBe(true)
    expect(result.current.isSaving).toBe(false)
    expect(result.current.error).toBeNull()
    expect(save).toHaveBeenCalledWith(['a'])
  })

  it('reports failure and sets an error message when the save rejects', async () => {
    const save = vi.fn().mockRejectedValue(new Error('network is down'))
    const { result } = renderHook(() => useSectionSave(['a'], save))

    let succeeded: boolean | undefined
    await act(async () => {
      succeeded = await result.current.handleSave()
    })

    expect(succeeded).toBe(false)
    expect(result.current.isSaving).toBe(false)
    expect(result.current.error).toBe('network is down')
  })

  it('serializes overlapping saves so the second PUT never starts before the first has settled', async () => {
    // Regression coverage for the two-overlapping-PUTs bug: without the queue in
    // useEditableCollection.ts, firing a Delete right before a Done could let the network
    // resolve them out of order and silently let a stale collection overwrite a newer one.
    const events: string[] = []
    let resolveFirst = () => {}
    const save = vi.fn((entries: string[]) => {
      events.push(`start:${entries.join(',')}`)
      if (entries.length === 1) {
        return new Promise<void>((resolve) => {
          resolveFirst = () => {
            events.push('end:a')
            resolve()
          }
        })
      }
      events.push(`end:${entries.join(',')}`)
      return Promise.resolve()
    })

    const { result } = renderHook(() => useSectionSave(['a'], save))

    let firstDone!: Promise<boolean>
    let secondDone!: Promise<boolean>
    act(() => {
      firstDone = result.current.handleSave(['a'])
      secondDone = result.current.handleSave(['a', 'b'])
    })

    // The second call is queued — its underlying save() must not run while the first is pending.
    await waitFor(() => expect(events).toEqual(['start:a']))

    await act(async () => {
      resolveFirst()
      await firstDone
    })

    await waitFor(() => expect(events).toEqual(['start:a', 'end:a', 'start:a,b', 'end:a,b']))

    await act(async () => {
      expect(await secondDone).toBe(true)
    })
  })
})
