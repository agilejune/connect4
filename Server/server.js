const io = require('socket.io')(process.env.PORT || 3000);
const shortid = require('shortid');

console.log('server started');

const players = new Map();
const _playerNameToIdMap = new Map();
const scores = {};
const games = new Map();

const controllerFactories = {
  'connect': require('./games/connect'),
};

const controllers = new Map();

initScore();

function initScore() {
  for (let i = 0; i < 20; ++i) {
    scores[`Player ${i+1}`] = 100 * i;
  }
}

function getScore(username) {
  return scores[username] || 0;
}

function setScore(username, score) {
  scores[username] = score;
}

io.on('connection', (socket) => {
  const thisPlayerId = socket.id;
  console.log(`Client connected: ${thisPlayerId}`);

  let thisPlayer;

  socket.on('disconnect', () => {
    console.log(`Client disconnected: ${thisPlayerId}`);

    if (!thisPlayer)
      return;

    if (!!thisPlayer.gameid) {
      leaveGame();
    }

    players.delete(thisPlayer.id);
    _playerNameToIdMap.delete(thisPlayer.name);

    io.emit('playerDestroyed', {
      id: thisPlayer.id
    });
    
    console.log(`Player deleted: player=(${thisPlayer.id}:${thisPlayer.name})`);
  });

  socket.on('login', (data, fn) => {
    const username = data.username;
    console.log(`Client try to login: player=(${thisPlayerId}:${username})`);

    if (!_playerNameToIdMap.has(username)) {
      thisPlayer = {
        id: thisPlayerId,
        name: username,
        score: getScore(username),
        gameid: null,
      };

      players.set(thisPlayer.id, thisPlayer);
      _playerNameToIdMap.set(thisPlayer.name, thisPlayer.id);

      socket.emit('updateScore', scores);

      for (const player of players.values()) {
        socket.emit('updatePlayer', player);
      }

      for (const game of games.values()) {
        socket.emit('updateGame', game);
      }
      
      io.emit('updatePlayer', thisPlayer);

      console.log(`Client login success: player=(${thisPlayer.id}:${thisPlayer.name})`);

      fn({
        result: true,
        playerid: thisPlayer.id
      });
    } else {
      console.log(`Client login failed. Player already logged in: ${thisPlayerId} with ${username}`);

      fn({
        result: false
      });
    }
  });

  socket.on('createGame', (data, fn) => {
    const gametype = data.type;
    const gamedata = data.data;

    console.log(`Player try to create game: player=(${thisPlayer.name}:${thisPlayer.id})`);

    if (!thisPlayer.gameid) {
      const game = {
        id: shortid.generate(),
        owner: thisPlayer.id,
        type: gametype,
        data: gamedata,
        players: [thisPlayer.id],
      };
      games.set(game.id, game);

      thisPlayer.gameid = game.id;

      const controller = createController(game);
      controllers.set(game.id, controller);

      socket.join(game.id, () => {
        controller.emit('join', thisPlayer, socket);
      });

      io.emit('updateGame', game);
      io.emit('updatePlayer', thisPlayer);

      console.log(`Player create game success: player=(${thisPlayer.id}:${thisPlayer.name}), game=${game.id}`);

      fn({
        result: true,
        gameid: game.id
      });
    } else {
      console.log(`Player create game failed: player=(${thisPlayer.id}:${thisPlayer.name}), game=${thisPlayer.gameid}`);

      fn({
        result: false
      });
    }
  });

  function createController(game) {
    const context = {
      game,
      io,

      updatePlayer(player) {
        io.emit('updatePlayer', player);

        if (getScore(player.name) != player.score) {
          setScore(player.name, player.score);
          io.emit('updateScore', scores);
        }
      },
    };
    return controllerFactories[game.type](context);
  }

  socket.on('joinGame', (data, fn) => {
    const gameid = data.gameid;
    const game = games.has(gameid) ? games.get(gameid) : null;
    const controller = controllers.get(gameid);

    console.log(`Player try to join game: player=(${thisPlayer.name}:${thisPlayer.id}), game=${gameid}`);

    if (!thisPlayer.gameid && !!game && !!controller && game.players.length < controller.maxPlayers) {
      game.players.push(thisPlayer.id);

      thisPlayer.gameid = game.id;
      socket.join(game.id, () => {
        controller.emit('join', thisPlayer, socket);
      });

      io.emit('updateGame', game);
      io.emit('updatePlayer', thisPlayer);

      console.log(`Player join game success: player=(${thisPlayer.id}:${thisPlayer.name}), game=${game.id}`);

      fn({
        result: true,
        gameid: game.id
      });
    } else {
      console.log(`Player join game failed: player=(${thisPlayer.name}:${thisPlayer.id}), game=${gameid}`);

      fn({
        result: false
      });
    }
  });

  socket.on('leaveGame', () => {
    leaveGame();
  });

  function leaveGame() {
    const gameid = thisPlayer.gameid;
    if (!gameid || !games.get(gameid))
      return;

    const game = games.get(gameid);
    game.players.splice(game.players.indexOf(thisPlayer.id), 1);
    io.emit('updateGame', game);

    thisPlayer.gameid = null;
    io.emit('updatePlayer', thisPlayer);

    socket.leave(game.id);

    if (controllers.has(game.id)) {
      const controller = controllers.get(game.id);
      controller.emit('leave', thisPlayer, socket);
    }

    console.log(`Player leave game: player=(${thisPlayer.name}:${thisPlayer.id}), game=${game.id}`);

    if (!game.players.length) {
      games.delete(game.id);
      controllers.delete(game.id);
      
      io.emit('gameDestroyed', {
        id: game.id
      });

      console.log(`Game destroy: game=${game.id}`);
    }
  }
})