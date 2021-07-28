const io = require('socket.io')(process.env.PORT || 3000);
const shortid = require('shortid');
const GameController = require('./game-controller');

console.log('server started');

const controller = new GameController();

io.on('connection', (socket) => {
  const thisPlayerId = shortid.generate();
  console.log(`Client connected: ${thisPlayerId}`);

  let thisPlayer;

  socket.on('disconnect', () => {
      console.log(`Client disconnected: ${thisPlayerId}`);

    if (!!thisPlayer) {
      if (!!thisPlayer.gameid) {
        leaveGame(thisPlayer);
      }

      controller.logout(thisPlayer);

      socket.broadcast.emit('playerDestroyed', { id: thisPlayer.id });
    }
  });

  socket.on('login', (data, fn) => {
    thisPlayer = controller.login(thisPlayerId, data.username);
    if (!!thisPlayer) {
      socket.emit('updateScore', controller.scores);

      for (const player of controller.players.values()) {
          socket.emit('updatePlayer', player);
      }

      for (const game of controller.games.values()) {
          socket.emit('updateGame', game);
      }

      socket.nsp.emit('updatePlayer', thisPlayer);

      fn({ result: true, playerid: thisPlayer.id });
    }
    else {
      fn({ result: false });
    }
  });

  socket.on('createGame', (data, fn) => {
    console.log(`Player try to create game: player=(${thisPlayer.name}:${thisPlayer.id})`);

    const game = controller.createGame(thisPlayer, data.rows, data.cols);
    if (!!game) {
      socket.nsp.emit('updateGame', game);
      socket.nsp.emit('updatePlayer', thisPlayer);

      fn({result: true, gameid: game.id });
    }
    else {
      fn({result: false});
    }
  });

  socket.on('joinGame', (data, fn) => {
    const game = controller.joinGame(thisPlayer, data.gameid);

    if (!!game) {
      socket.broadcast.emit('updateGame', game);
      socket.nsp.emit('updatePlayer', thisPlayer);
      
      fn({ result: true, gameid: game.id });
    }
    else {
      fn({ result: false });
    }
  });

  socket.on('leaveGame', () => {
    leaveGame();
  });

  function leaveGame() {
    const game = controller.leaveGame(thisPlayer);

    socket.nsp.emit('updatePlayer', thisPlayer);
    socket.nsp.emit('updateGame', game);

    if (!game.players.length) {
        socket.nsp.emit('gameDestroyed', {
            id: game.id
        });
    }
  }
})
