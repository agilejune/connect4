class GameLogic {
    constructor(rows, cols) {
        this.rows = rows;
        this.cols = cols;
        this.cells = new Array(rows * cols);
        this.cells.fill(0);
    }

    getCell(r, c) { return this.cells[this.cols*r + c]; }
    setCell(r, c, player) { this.cells[this.cols*r + c] = player; }

    drop(player, col) {
        for (let r = 0; r < this.rows; ++r) {
            if (this.getCell(r, col) === 0) {
                this.setCell(r, col, player);
                return r;
            }
        }
        return -1;
    }

    listConnects(player, count) {
        let allConnects = [];
        for (let r = 0; r < this.rows; ++r) {
            for (let c = 0; c < this.rows; ++c) {
                let connects = this._listConnectsFrom(r, c, 1, 0, player);
                if (connects.length >= count) {
                    allConnects.push(...connects);
                }
                connects = this._listConnectsFrom(r, c, 0, 1, player);
                if (connects.length >= count) {
                    allConnects.push(...connects);
                }
                connects = this._listConnectsFrom(r, c, 1, 1, player);
                if (connects.length >= count) {
                    allConnects.push(...connects);
                }
                connects = this._listConnectsFrom(r, c, 1, -1, player);
                if (connects.length >= count) {
                    allConnects.push(...connects);
                }
            }
        }
        return [...new Set(allConnects)];
    }

    _listConnectsFrom(r, c, dx, dy, player) {
        const connects = [];
        for (; r >= 0 && r < this.rows && c >= 0 && c < this.cols; r += dy, c += dx) {
            if (this.getCell(r, c) !== player)
                break;
            connects.push({r, c});
        }
        return connects;
    }
}

module.exports = GameLogic;
