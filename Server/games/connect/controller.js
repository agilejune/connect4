const {
    EventEmitter
} = require('events');
const GameLogic = require('./connect');

module.exports = (context) => {
    const controller = new EventEmitter();
    controller.maxPlayers = 2;

    const game = context.game;
    const io = context.io;

    const connectCount = game.data.n || 4;

    let turn;
    let logic;
    let win = 0;

    let state = 'init';
    const ready = new Array(controller.maxPlayers);

    controller.on('join', (player, socket) => {
        if (game.players.indexOf(player.id) < 0)
            return;

        let playerIndex;

        socket.on('playerReady', () => {
            ready.fill(false);

            io.in(game.id).emit('_join', { playerid: player.id });
    
            if (game.players.length === controller.maxPlayers) {
                state = 'ready';
                io.in(game.id).emit('_ready');
            }
        });

        socket.on('_ready', () => {
            if (state !== 'ready')
                return;

            playerIndex = game.players.indexOf(player.id);
            ready[playerIndex] = true;

            console.log(`Game: ${game.id}, Player: ${player.id}, ready`);

            if (ready.every(v=>v)) {
                ready.fill(false);

                logic = new GameLogic(game.data.rows || 6, game.data.cols || 7);
                turn = win;

                state = 'play';

                io.in(game.id).emit('_start');
                io.in(game.id).emit('_turn', {
                    turn: turn
                });

                console.log(`Game: ${game.id}, Player: ${player.id}, start`);
            }
        });

        socket.on('_drop', (col) => {
            if (state !== 'play')
                return;

            console.log(`Game: ${game.id}, Player: ${player.id}, Event: drop: ${col}`);

            if (turn != playerIndex)
                return;

            const row = logic.drop(turn + 1, col);
            io.in(game.id).emit('_drop', {
                playerid: player.id,
                col,
                row
            });
        
            let connects = logic.listConnects(turn + 1, connectCount);
            if (connects.length >= connectCount) {
                win = turn;
                console.log(`Game win: ${win}`);
                io.in(game.id).emit('_end', {
                    winner: player.id,
                    connects
                });
                state = 'ready';

                player.score += 1;

                context.updatePlayer(player);
            }
            else {
                turn = (turn + 1) % controller.maxPlayers;
                io.in(game.id).emit('_turn', {
                    turn
                });
            }
        })
    });

    controller.on('leave', (player, socket) => {
        socket.removeAllListeners('_ready');
        socket.removeAllListeners('_drop');

        state = 'init';
        io.in(game.id).emit('_leave', { playerid: player.id });
    });

    return controller;
}