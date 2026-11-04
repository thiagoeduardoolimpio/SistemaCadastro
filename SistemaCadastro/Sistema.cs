using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace SistemaCadastro
{
    public partial class Sistema : Form
    {
        private readonly string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;
        private int bandaSelecionada = -1;

        public Sistema()
        {
            InitializeComponent();
        }

        private MySqlConnection CriarConexao()
        {
            return new MySqlConnection(connectionString);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCadastra_Click(object sender, EventArgs e)
        {
            marcador.Height = btnCadastra.Height;
            marcador.Top = btnCadastra.Top;
            tabControl1.SelectedTab = tabControl1.TabPages[0];
        }

        private void btnBusca_Click(object sender, EventArgs e)
        {
            marcador.Height = btnBusca.Height;
            marcador.Top = btnBusca.Top;
            tabControl1.SelectedTab = tabControl1.TabPages[1];
            ListarBandas();
        }

        private void Sistema_Load(object sender, EventArgs e)
        {
            try
            {
                CarregarGeneros();
                ListarBandas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível conectar ao MySQL. Confira se o servidor está ligado, se o banco 'cadastro' foi criado e se a senha no App.config está correta.\n\nDetalhes: " + ex.Message,
                    "Erro de conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CarregarGeneros()
        {
            using (MySqlConnection conexao = CriarConexao())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter("SELECT idgenero, genero FROM generos ORDER BY genero", conexao))
            {
                DataTable tabela = new DataTable();
                adaptador.Fill(tabela);
                cbGenero.DataSource = tabela.Copy();
                cbGenero.DisplayMember = "genero";
                cbGenero.ValueMember = "idgenero";
                cbAlteraGenero.DataSource = tabela;
                cbAlteraGenero.DisplayMember = "genero";
                cbAlteraGenero.ValueMember = "idgenero";
                cbGenero.SelectedIndex = tabela.Rows.Count > 0 ? 0 : -1;
                cbAlteraGenero.SelectedIndex = tabela.Rows.Count > 0 ? 0 : -1;
            }
        }

        private void ListarBandas()
        {
            string sql = "SELECT b.idbandas, b.nome, b.integrantes, b.ranking, g.genero " +
                         "FROM bandas b INNER JOIN generos g ON g.idgenero = b.fk_genero " +
                         "WHERE b.nome LIKE @busca ORDER BY b.nome";
            using (MySqlConnection conexao = CriarConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
            {
                comando.Parameters.AddWithValue("@busca", "%" + txtBusca.Text.Trim() + "%");
                DataTable tabela = new DataTable();
                adaptador.Fill(tabela);
                dgBandas.DataSource = tabela;
                if (dgBandas.Columns.Contains("idbandas")) dgBandas.Columns["idbandas"].Visible = false;
                if (dgBandas.Columns.Contains("integrantes")) dgBandas.Columns["integrantes"].HeaderText = "Integrantes";
                if (dgBandas.Columns.Contains("nome")) dgBandas.Columns["nome"].HeaderText = "Banda";
                if (dgBandas.Columns.Contains("ranking")) dgBandas.Columns["ranking"].HeaderText = "Ranking";
                if (dgBandas.Columns.Contains("genero")) dgBandas.Columns["genero"].HeaderText = "Gênero";
            }
        }

        private void txtBusca_TextChanged(object sender, EventArgs e)
        {
            try { ListarBandas(); }
            catch (Exception ex) { lblMensagem.Text = "Erro ao buscar: " + ex.Message; }
        }

        private void btnRemoveBanda_Click(object sender, EventArgs e)
        {
            if (dgBandas.CurrentRow == null || dgBandas.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Selecione uma banda na lista antes de remover.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int id = Convert.ToInt32(dgBandas.CurrentRow.Cells["idbandas"].Value);
            string nome = Convert.ToString(dgBandas.CurrentRow.Cells["nome"].Value);
            if (MessageBox.Show("Deseja realmente remover a banda '" + nome + "'?", "Confirmar remoção", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using (MySqlConnection conexao = CriarConexao())
                using (MySqlCommand comando = new MySqlCommand("DELETE FROM bandas WHERE idbandas = @id", conexao))
                {
                    comando.Parameters.AddWithValue("@id", id);
                    conexao.Open();
                    comando.ExecuteNonQuery();
                }
                ListarBandas();
                lblMensagem.Text = "Banda removida.";
            }
            catch (Exception ex) { MessageBox.Show("Não foi possível remover a banda.\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void btnAlterar_Click(object sender, EventArgs e)
        {
            if (dgBandas.CurrentRow == null || dgBandas.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Selecione uma banda para alterar.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bandaSelecionada = Convert.ToInt32(dgBandas.CurrentRow.Cells["idbandas"].Value);
            try
            {
                using (MySqlConnection conexao = CriarConexao())
                using (MySqlCommand comando = new MySqlCommand("SELECT nome, integrantes, ranking, fk_genero FROM bandas WHERE idbandas = @id", conexao))
                {
                    comando.Parameters.AddWithValue("@id", bandaSelecionada);
                    conexao.Open();
                    using (MySqlDataReader leitor = comando.ExecuteReader())
                    {
                        if (!leitor.Read()) return;
                        txtAlteraNome.Text = leitor["nome"].ToString();
                        txtAlteraIntegrantes.Text = leitor["integrantes"].ToString();
                        txtAlteraRanking.Text = leitor["ranking"].ToString();
                        cbAlteraGenero.SelectedValue = Convert.ToInt32(leitor["fk_genero"]);
                    }
                }
                tabControl1.SelectedTab = tabAlterar;
            }
            catch (Exception ex) { MessageBox.Show("Não foi possível carregar os dados da banda.\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void btnConfirmaAlteracao_Click(object sender, EventArgs e)
        {
            int integrantes, ranking;
            if (bandaSelecionada < 0) { MessageBox.Show("Selecione uma banda na aba Buscar."); return; }
            if (string.IsNullOrWhiteSpace(txtAlteraNome.Text) || txtAlteraNome.Text.Trim().Length > 45 || !int.TryParse(txtAlteraIntegrantes.Text, out integrantes) || integrantes < 1 ||
                !int.TryParse(txtAlteraRanking.Text, out ranking) || ranking < 1 || cbAlteraGenero.SelectedValue == null)
            {
                MessageBox.Show("Informe o nome (até 45 caracteres), a quantidade de integrantes e o ranking (números positivos), além do gênero.", "Dados inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (MySqlConnection conexao = CriarConexao())
                using (MySqlCommand comando = new MySqlCommand("UPDATE bandas SET nome=@nome, integrantes=@integrantes, ranking=@ranking, fk_genero=@genero WHERE idbandas=@id", conexao))
                {
                    comando.Parameters.AddWithValue("@nome", txtAlteraNome.Text.Trim());
                    comando.Parameters.AddWithValue("@integrantes", integrantes);
                    comando.Parameters.AddWithValue("@ranking", ranking);
                    comando.Parameters.AddWithValue("@genero", Convert.ToInt32(cbAlteraGenero.SelectedValue));
                    comando.Parameters.AddWithValue("@id", bandaSelecionada);
                    conexao.Open();
                    comando.ExecuteNonQuery();
                }
                ListarBandas();
                tabControl1.SelectedTab = tabBuscar;
                lblMensagem.Text = "Alterações salvas.";
                bandaSelecionada = -1;
            }
            catch (Exception ex) { MessageBox.Show("Não foi possível alterar a banda.\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void bntAddGenero_Click(object sender, EventArgs e)
        {
            string genero = SolicitarTexto("Digite o nome do novo gênero musical:", "Adicionar gênero");
            if (string.IsNullOrWhiteSpace(genero)) return;
            if (genero.Trim().Length > 45) { MessageBox.Show("O gênero deve ter no máximo 45 caracteres."); return; }
            try
            {
                using (MySqlConnection conexao = CriarConexao())
                using (MySqlCommand comando = new MySqlCommand("INSERT INTO generos (genero) VALUES (@genero)", conexao))
                {
                    comando.Parameters.AddWithValue("@genero", genero.Trim());
                    conexao.Open();
                    comando.ExecuteNonQuery();
                }
                CarregarGeneros();
                cbGenero.SelectedIndex = cbGenero.FindStringExact(genero.Trim());
                lblMensagem.Text = "Gênero adicionado.";
            }
            catch (Exception ex) { MessageBox.Show("Não foi possível adicionar o gênero.\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private string SolicitarTexto(string mensagem, string titulo)
        {
            using (Form janela = new Form())
            using (Label rotulo = new Label())
            using (TextBox entrada = new TextBox())
            using (Button confirmar = new Button())
            using (Button cancelar = new Button())
            {
                janela.Text = titulo;
                janela.FormBorderStyle = FormBorderStyle.FixedDialog;
                janela.StartPosition = FormStartPosition.CenterParent;
                janela.ClientSize = new Size(360, 125);
                janela.MinimizeBox = false;
                janela.MaximizeBox = false;
                rotulo.Text = mensagem;
                rotulo.AutoSize = true;
                rotulo.Location = new Point(12, 12);
                entrada.Location = new Point(12, 40);
                entrada.Width = 335;
                confirmar.Text = "OK";
                confirmar.DialogResult = DialogResult.OK;
                confirmar.Location = new Point(186, 78);
                cancelar.Text = "Cancelar";
                cancelar.DialogResult = DialogResult.Cancel;
                cancelar.Location = new Point(267, 78);
                janela.Controls.AddRange(new Control[] { rotulo, entrada, confirmar, cancelar });
                janela.AcceptButton = confirmar;
                janela.CancelButton = cancelar;
                return janela.ShowDialog(this) == DialogResult.OK ? entrada.Text : null;
            }
        }

        private void BtnConfirmaCadastro_Click(object sender, EventArgs e)
        {
            int integrantes, ranking;
            if (string.IsNullOrWhiteSpace(txtnome.Text) || txtnome.Text.Trim().Length > 45 ||
                !int.TryParse(txtintegrantes.Text, out integrantes) || integrantes < 1 ||
                !int.TryParse(txtranking.Text, out ranking) || ranking < 1 || cbGenero.SelectedValue == null)
            {
                MessageBox.Show("Preencha o nome (até 45 caracteres), a quantidade de integrantes e o ranking com números positivos e selecione um gênero.", "Dados inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                using (MySqlConnection conexao = CriarConexao())
                using (MySqlCommand comando = new MySqlCommand("sp_insereBanda", conexao))
                {
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.Parameters.AddWithValue("nome", txtnome.Text.Trim());
                    comando.Parameters.AddWithValue("integrantes", integrantes);
                    comando.Parameters.AddWithValue("ranking", ranking);
                    comando.Parameters.AddWithValue("genero", Convert.ToInt32(cbGenero.SelectedValue));
                    conexao.Open();
                    comando.ExecuteNonQuery();
                }
                txtnome.Clear();
                txtintegrantes.Clear();
                txtranking.Clear();
                ListarBandas();
                lblMensagem.Text = "Banda cadastrada com sucesso!";
                MessageBox.Show("Banda cadastrada com sucesso!", "Cadastro", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Não foi possível cadastrar a banda. Confira a conexão e o banco de dados.\n" + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
