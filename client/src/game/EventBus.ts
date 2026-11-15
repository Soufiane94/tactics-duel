/*
 * A typed publish/subscribe channel: senders and listeners agree on message names, not on each other. GameScene and
 * HudScene talk through one (`hud/hudEvents.ts` lists their messages), so neither scene imports the other.
 * Like a C# event with a typed delegate per message, or one RxJS Subject per message. Phaser has its own
 * EventEmitter, but its `emit(name, ...args: any[])` checks nothing at compile time. This one is typed, and has no
 * Phaser import, so Vitest tests it.
 */

/**
 * The channel. `Messages` maps each message name to the arguments its listeners receive, as a tuple:
 * `{ toast: [text: string] }`. A wrong name or a wrong argument is a compile error, at the sender and the listener.
 */
export class EventBus<Messages extends { [Name in keyof Messages]: readonly unknown[] }> {
  /** The listeners of each message name. A Set, so the same listener added twice still runs once per message. */
  private listeners: { [Name in keyof Messages]?: Set<(...args: Messages[Name]) => void> } = {};

  /**
   * Calls `listener` with the arguments of every `name` message sent from now on. Messages sent before are lost:
   * there's no replay. Returns the function that stops listening (like disposing an `IDisposable` subscription).
   */
  on<Name extends keyof Messages>(name: Name, listener: (...args: Messages[Name]) => void): () => void {
    const listeners = (this.listeners[name] ??= new Set());
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  }

  /**
   * Sends a message to everyone listening to `name`, synchronously, in the order they started listening. Returns
   * false if nobody was listening, so a sender that waits for an answer can tell it will never come.
   */
  emit<Name extends keyof Messages>(name: Name, ...args: Messages[Name]): boolean {
    const listeners = this.listeners[name];
    if (listeners === undefined || listeners.size === 0) {
      return false;
    }
    // A copy: a listener may stop listening (itself or another one) while the message is being delivered.
    for (const listener of [...listeners]) {
      listener(...args);
    }
    return true;
  }

  /** Removes every listener, for example when the scenes that listen shut down. */
  clear(): void {
    this.listeners = {};
  }
}
