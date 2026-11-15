import type Phaser from "phaser";
import { HUD_SHAPES, HUD_TEXT, PLACEHOLDER_THEME } from "../media/placeholder";
import type { UnitPanelModel } from "./hudModel";

/**
 * The unit card on the left of the screen: a swatch in the owner's colour and the type's name, then one line per
 * stat. It draws the `UnitPanelModel` it's given, and computes nothing (`hudModel.ts` does).
 * Its parts sit in one Container, so showing, hiding or moving the panel is one call.
 */
export class UnitPanel {
  private readonly container: Phaser.GameObjects.Container;
  private readonly background: Phaser.GameObjects.Rectangle;
  private readonly swatch: Phaser.GameObjects.Rectangle;
  private readonly title: Phaser.GameObjects.Text;
  private readonly lines: Phaser.GameObjects.Text;

  /** `x`, `y`: the panel's top-left corner, in screen pixels. */
  constructor(scene: Phaser.Scene, x: number, y: number) {
    const { width, padding, colour, alpha } = HUD_SHAPES.panel;
    const swatchSize = HUD_SHAPES.swatchSize;

    // Origin (0, 0) for the boxes: they're laid out from their top-left corners, like boxes in a page. The final
    // places and sizes depend on the texts, so `show` sets them once the texts are in.
    this.background = scene.add.rectangle(0, 0, width, 1, colour, alpha).setOrigin(0, 0);
    this.swatch = scene.add.rectangle(padding, padding, swatchSize, swatchSize, 0xffffff).setOrigin(0, 0.5);
    this.title = scene.add.text(padding + swatchSize + 8, padding, "", HUD_TEXT.panelTitle);
    this.lines = scene.add.text(padding, padding, "", HUD_TEXT.panelLines);

    this.container = scene.add
      .container(x, y, [this.background, this.swatch, this.title, this.lines])
      .setVisible(false);
  }

  /** Shows a unit's card, or hides the panel for null. */
  show(panel: UnitPanelModel | null): void {
    if (panel === null) {
      this.container.setVisible(false);
      return;
    }
    const { width, padding } = HUD_SHAPES.panel;
    this.title.setText(panel.title);
    this.lines.setText(panel.lines.join("\n"));

    // A Text's width and height are measured from its text, so the layout comes after `setText`.
    this.swatch.setFillStyle(PLACEHOLDER_THEME.team[panel.owner]).setY(this.title.y + this.title.height / 2);
    this.lines.setY(this.title.y + this.title.height + 10);
    // The background grows with the text, so a unit type with a long name or more stats still fits.
    const textRight = Math.max(this.title.x + this.title.width, this.lines.x + this.lines.width) + padding;
    this.background.setSize(Math.max(width, textRight), this.lines.y + this.lines.height + padding);
    this.container.setVisible(true);
  }
}
